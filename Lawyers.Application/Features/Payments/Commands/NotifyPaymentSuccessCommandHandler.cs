using Lawyers.Application.Features.Consultation.Events; // 👈 Add this
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR; // 👈 Needed for IPublisher
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Payments.Commands;

public class NotifyPaymentSuccessCommandHandler : IRequestHandler<NotifyPaymentSuccessCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IPublisher _publisher; // 👈 ADD THIS

    public NotifyPaymentSuccessCommandHandler(
        IUnitOfWork unitOfWork, 
        INotificationService notificationService,
        IPublisher publisher) // 👈 ADD THIS
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _publisher = publisher; // 👈 ADD THIS
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

        // 6. Direct Notification (Optional but good for immediate email/SMS)
        await _notificationService.SendBookingConfirmedAsync(
            consultation.Client.UserId,
            consultation.Lawyer.UserId,
            consultation.ScheduledAt);

        // 🚀 7. PUBLISH THE DOMAIN EVENT (THIS IS THE MISSING PIECE!) 🚀
        // This triggers your ConsultationConfirmedSignalRHandler to push the 
        // real-time notification to the frontend via SignalR.
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