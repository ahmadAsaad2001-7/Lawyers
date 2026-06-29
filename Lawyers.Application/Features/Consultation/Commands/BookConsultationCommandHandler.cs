using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Consultations.Commands;

public class BookConsultationCommandHandler : IRequestHandler<BookConsultationCommand, BookingResponseDto>
{
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

        var lawyer = await _unitOfWork.LawyerProfiles.GetByIdAsync(request.LawyerId, cancellationToken);
        if (lawyer == null || !lawyer.IsVerified)
        {
            throw new Exception("Lawyer not found or not verified.");
        }

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

        var totalCost = lawyer.HourlyRate * (request.DurationMinutes / 60m);

        // Explicit transactional boundaries applied cleanly to our UOW
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var consultation = new Consultation
            {
                ClientId = _currentUserService.UserId.Value,
                LawyerId = request.LawyerId,
                ScheduledAt = request.ScheduledAt,
                DurationMinutes = request.DurationMinutes,
                Status = ConsultationStatus.Pending 
            };

            await _unitOfWork.Consultations.AddAsync(consultation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); 

            // Trigger Kashier's 'Authorize' intent loop
            var paymentResult = await _paymentService.CreatePaymentIntentAsync(
                totalCost,
                "EGP",
                consultation.Id,
                _currentUserService.Email ?? "client@lawyerplatform.com"
            );

            var payment = new Payment
            {
                ConsultationId = consultation.Id,
                ClientId = consultation.ClientId,
                LawyerId = consultation.LawyerId,
                StripePaymentIntentId = paymentResult.PaymentIntentId, // Mapped to string ID field
                Amount = totalCost,
                Currency = "EGP",
                Status = PaymentStatus.Pending
            };

            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
            
            // Commits both records atomically safely wrapped inside the DB transaction
            await _unitOfWork.CommitTransactionAsync(); 

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
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}