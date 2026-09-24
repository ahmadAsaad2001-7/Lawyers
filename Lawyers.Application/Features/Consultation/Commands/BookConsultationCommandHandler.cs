using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Lawyers.Application.Features.LawyerSchedule.Queries;
namespace Lawyers.Application.Features.Consultations.Commands;

public class BookConsultationCommandHandler : IRequestHandler<BookConsultationCommand, BookingResponseDto>
{
    private const string PaymentCurrency = "EGP";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPaymentService _paymentService;
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    
    public BookConsultationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPaymentService paymentService,
        IMediator mediator,
        INotificationService notificationService
        )
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
        _mediator = mediator;
        _notificationService = notificationService;
    }

    public async Task<BookingResponseDto> Handle(BookConsultationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == null)
        {
            throw new UnauthorizedAccessException("You must be logged in to book a consultation.");
        }

        // ✅ Ensure duration is a multiple of 60 (matches the new hourly selection UI)
        if (request.DurationMinutes <= 0 || request.DurationMinutes % 60 != 0)
        {
            throw new ArgumentException("Consultation duration must be greater than zero and a multiple of 60 minutes.", nameof(request.DurationMinutes));
        }

        var lawyer = await _unitOfWork.LawyerProfiles.GetByIdAsync(request.LawyerId, cancellationToken);
        if (lawyer == null || !lawyer.IsVerified)
        {
            throw new Exception("Lawyer not found or not verified.");
        }

        // ✅ Admin auto-creates a client profile so they can occupy the ClientId slot
        var clientProfile = await _unitOfWork.ClientProfiles.Query()
            .FirstOrDefaultAsync(client => client.UserId == _currentUserService.UserId.Value, cancellationToken);

        if (clientProfile == null && _currentUserService.IsAdmin)
        {
            clientProfile = new ClientProfile
            {
                UserId = _currentUserService.UserId.Value,
                FullName = "Admin"
            };
            await _unitOfWork.ClientProfiles.AddAsync(clientProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (clientProfile == null)
        {
            throw new Exception("Client profile not found.");
        }

        // 🛡️ ADMIN BYPASS: no overlap check, no availability check, no payment, instantly Confirmed
        if (_currentUserService.IsAdmin)
        {
            var adminConsultation = new Domain.Entities.Consultation
            {
                ClientId = clientProfile!.Id,
                LawyerId = request.LawyerId,
                ScheduledAt = request.ScheduledAt,
                DurationMinutes = request.DurationMinutes,
                Status = ConsultationStatus.Confirmed
            };

            await _unitOfWork.Consultations.AddAsync(adminConsultation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _notificationService.SendBookingConfirmedAsync(
                clientProfile.UserId, lawyer.UserId, adminConsultation.ScheduledAt, adminConsultation.Id);

            return new BookingResponseDto
            {
                ConsultationId = adminConsultation.Id,
                LawyerId = lawyer.Id,
                ScheduledAt = adminConsultation.ScheduledAt,
                Status = adminConsultation.Status.ToString(),
                TotalCost = 0,
                PaymentClientSecret = null   // ✅ no Kashier redirect
            };
        }

        // ═══════════════════════════════════════════════════
        // Normal client flow continues below
        // ═══════════════════════════════════════════════════

        var totalCost = lawyer.HourlyRate * (request.DurationMinutes / 60m);
        var consultation = await ReserveConsultationSlotAsync(request, clientProfile.Id, cancellationToken);

        // 🟢 1. Build the InitiatePaymentDto for Kashier
        var initiateDto = new InitiatePaymentDto(
            Amount: totalCost,
            Currency: PaymentCurrency,
            ConsultationId: consultation.Id,
            CustomerEmail: _currentUserService.Email ?? "client@lawyerplatform.com",
            Channel: request.Channel,
            CardToken: request.CardToken,
            WalletPhoneNumber: request.WalletPhoneNumber
        );

        PaymentIntentResponseDto paymentResult;
        try
        {
            // 🟢 2. Send multi-channel request to Payment Gateway
            paymentResult = await _paymentService.CreatePaymentIntentAsync(initiateDto);
        }
        catch
        {
            await CancelReservedConsultationAsync(consultation.Id, cancellationToken);
            throw;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var payment = new Payment
            {
                ConsultationId = consultation.Id,
                ClientId = consultation.ClientId,
                LawyerId = consultation.LawyerId,
                TransactionId = paymentResult.PaymentIntentId,
                Amount = totalCost,
                Currency = PaymentCurrency,
                Status = PaymentStatus.Pending
            };

            // 🔗 wire the payment into the consultation
            consultation.Payment = payment;

            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            await _paymentService.CancelPaymentAsync(paymentResult.PaymentIntentId);
            await CancelReservedConsultationAsync(consultation.Id, cancellationToken);
            throw;
        }

        await _notificationService.SendNewBookingAsync(
            clientProfile.UserId, lawyer.UserId, consultation.ScheduledAt, consultation.Id);

        return new BookingResponseDto
        {
            ConsultationId = consultation.Id,
            LawyerId = lawyer.Id,
            ScheduledAt = consultation.ScheduledAt,
            Status = consultation.Status.ToString(),
            TotalCost = totalCost,
            PaymentClientSecret = paymentResult.ClientSecret
        };
    }

  private async Task<Domain.Entities.Consultation> ReserveConsultationSlotAsync(
    BookConsultationCommand request,
    int clientProfileId,
    CancellationToken cancellationToken)
{
    await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

    try
    {
        TimeZoneInfo localTimeZone;
        try
        {
            localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch
        {
            localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }

        var localRequestedStart = TimeZoneInfo.ConvertTimeFromUtc(request.ScheduledAt, localTimeZone);
        var utcRequestedEnd = request.ScheduledAt.AddMinutes(request.DurationMinutes);
        var hoursNeeded = request.DurationMinutes / 60;

        var requestedHours = Enumerable.Range(0, hoursNeeded)
            .Select(i => localRequestedStart.Hour + i)
            .ToList();

        // ✅ REPLACED: no longer queries the empty LawyerAvailabilities table.
        // Uses the same computation (weekly schedule + exceptions + existing
        // bookings) that GetActualAvailabilityQueryHandler uses for the picker,
        // so the two can never disagree again.
        var actualAvailableHours = await _mediator.Send(
            new GetActualAvailabilityQuery(
                request.LawyerId, localRequestedStart.Date),
            cancellationToken);

        var missingHours = requestedHours.Except(actualAvailableHours).ToList();
        if (missingHours.Count > 0)
        {
            throw new Exception($"المحامي غير متاح طوال المدة المطلوبة. الساعات غير المتاحة: {string.Join(", ", missingHours.Select(h => $"{h}:00"))}");
        }

        var isOverlapping = await _unitOfWork.Consultations.Query()
            .AnyAsync(c =>
                c.LawyerId == request.LawyerId &&
                c.Status != ConsultationStatus.Cancelled &&
                c.ScheduledAt < utcRequestedEnd &&
                c.ScheduledAt.AddMinutes(c.DurationMinutes) > request.ScheduledAt,
                cancellationToken);

        if (isOverlapping)
        {
            throw new Exception("هذا الموعد لم يعد متاحاً (تم حجزه من قبل شخص آخر).");
        }

        var consultation = new Domain.Entities.Consultation
        {
            ClientId = clientProfileId,
            LawyerId = request.LawyerId,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Status = ConsultationStatus.Pending
        };

        await _unitOfWork.Consultations.AddAsync(consultation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync();

        return consultation;
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}

    private async Task CancelReservedConsultationAsync(int consultationId, CancellationToken cancellationToken)
    {
        var consultation = await _unitOfWork.Consultations.GetByIdAsync(consultationId, cancellationToken);
        if (consultation == null || consultation.Status != ConsultationStatus.Pending)
        {
            return;
        }

        consultation.Status = ConsultationStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}