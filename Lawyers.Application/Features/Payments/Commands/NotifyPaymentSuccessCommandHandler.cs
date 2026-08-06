using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Payments.Commands;

public class NotifyPaymentSuccessCommandHandler : IRequestHandler<NotifyPaymentSuccessCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public NotifyPaymentSuccessCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(NotifyPaymentSuccessCommand request, CancellationToken cancellationToken)
    {
        if (!IsSuccessfulPaymentStatus(request.Status))
        {
            return true;
        }

        var consultation = await _unitOfWork.Consultations.Query()
            .Include(c => c.Payment)
            .FirstOrDefaultAsync(c => c.Id == request.ConsultationId, cancellationToken);

        if (consultation == null || consultation.Status != ConsultationStatus.Pending)
        {
            return true;
        }

        consultation.Status = ConsultationStatus.Confirmed;

        if (consultation.Payment != null)
        {
            if (!string.IsNullOrWhiteSpace(consultation.Payment.TransactionId) &&
                consultation.Payment.TransactionId != request.TransactionId)
            {
                throw new InvalidOperationException("Webhook transaction id does not match the stored payment transaction id.");
            }

            consultation.Payment.Status = PaymentStatus.Succeeded;
            consultation.Payment.TransactionId = request.TransactionId;
            consultation.Payment.TransactionDate = DateTime.UtcNow;
        }
        else
        {
            throw new InvalidOperationException("Cannot confirm consultation because no payment record exists.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ⚠️ see note below about ClientId/LawyerId vs UserId
        await _notificationService.SendBookingConfirmedAsync(
            consultation.ClientId, consultation.LawyerId, consultation.ScheduledAt);

        return true;
    }

    private static bool IsSuccessfulPaymentStatus(string status) =>
        status.Trim().ToUpperInvariant() is "SUCCESS" or "CAPTURED" or "AUTHORIZED";
}