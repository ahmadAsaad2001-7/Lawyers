using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Lawyers.Application.Features.Consultations.Commands;

public class BookConsultationCommandHandler : IRequestHandler<BookConsultationCommand, BookingResponseDto>
{
    private const string PaymentCurrency = "EGP";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPaymentService _paymentService;

    public BookConsultationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPaymentService paymentService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
    }

    public async Task<BookingResponseDto> Handle(BookConsultationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == null)
        {
            throw new UnauthorizedAccessException("You must be logged in to book a consultation.");
        }

        if (request.DurationMinutes <= 0)
        {
            throw new ArgumentException("Consultation duration must be greater than zero.", nameof(request.DurationMinutes));
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

        // 🛡️ ADMIN BYPASS: no overlap check, no payment, instantly Confirmed
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
        // Normal client flow continues below (unchanged)
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
            var requestedStart = request.ScheduledAt;
            var requestedEnd = request.ScheduledAt.AddMinutes(request.DurationMinutes);

            var isOverlapping = await _unitOfWork.Consultations.Query()
                .AnyAsync(c =>
                    c.LawyerId == request.LawyerId &&
                    c.Status != ConsultationStatus.Cancelled &&
                    c.ScheduledAt < requestedEnd &&
                    c.ScheduledAt.AddMinutes(c.DurationMinutes) > requestedStart,
                    cancellationToken);

            if (isOverlapping)
            {
                throw new Exception("This lawyer is already booked at the requested time.");
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