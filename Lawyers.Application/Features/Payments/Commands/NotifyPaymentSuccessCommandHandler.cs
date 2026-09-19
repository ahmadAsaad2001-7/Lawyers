using Lawyers.Application.Features.Consultation.Events;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Payments.Commands;

public class NotifyPaymentSuccessCommandHandler : IRequestHandler<NotifyPaymentSuccessCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public NotifyPaymentSuccessCommandHandler(
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<bool> Handle(NotifyPaymentSuccessCommand request, CancellationToken cancellationToken)
    {
        // 1. Non-success → acknowledge and stop
        if (!IsSuccessfulPaymentStatus(request.Status))
            return true;

        // 2. Load consultation + payment + profiles
        var consultation = await _unitOfWork.Consultations.Query()
            .Include(c => c.Payment)
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .FirstOrDefaultAsync(c => c.Id == request.ConsultationId, cancellationToken);

        if (consultation is null) return true;

        // 3. Idempotency
        if (consultation.Status != ConsultationStatus.Pending) return true;

        // 4. Resolve the payment
        var payment = consultation.Payment;
        if (payment is null)
        {
            payment = await _unitOfWork.Payments.Query()
                .FirstOrDefaultAsync(p => p.ConsultationId == consultation.Id, cancellationToken);
            if (payment is null) return false;
            consultation.Payment = payment;
        }

        // 5. Update payment + Domain Logic
        payment.Status = PaymentStatus.Succeeded;
        payment.TransactionDate = DateTime.UtcNow;
        consultation.MarkAsConfirmed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Publish the domain event — the SOLE notification trigger.
        // ConsultationConfirmedSignalRHandler subscribes to this and calls
        // INotificationService.SendBookingConfirmedAsync itself. A direct call
        // here as well would double-send the same push to both parties.
        await _publisher.Publish(new ConsultationConfirmedEvent
        {
            ConsultationId = consultation.Id,
            ClientId = consultation.Client.UserId,
            LawyerId = consultation.Lawyer.UserId,
            ScheduledAt = consultation.ScheduledAt
        }, cancellationToken);

        return true;
    }

    private static bool IsSuccessfulPaymentStatus(string status) =>
        status.Trim().ToUpperInvariant() is "SUCCESS" or "CAPTURED" or "AUTHORIZED" or "PAID";
}