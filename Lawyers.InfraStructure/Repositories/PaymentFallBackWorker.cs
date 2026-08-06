using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using Lawyers.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lawyers.Infrastructure.Services;

public class PaymentFallbackWorker
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentFallbackWorker> _logger;

    public PaymentFallbackWorker(
        AppDbContext context,
        IPaymentService paymentService,
        INotificationService notificationService,
        ILogger<PaymentFallbackWorker> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task CheckAndConfirmPaymentStatusAsync(int consultationId)
    {
        var consultation = await _context.Consultations
            .Include(c => c.Payment)
            .FirstOrDefaultAsync(c => c.Id == consultationId);

        if (consultation?.Payment == null || consultation.Status != ConsultationStatus.Pending)
        {
            _logger.LogWarning("Consultation {Id} not found or already processed.", consultationId);
            return;
        }

        var gatewayStatus = await _paymentService.GetPaymentStatusAsync(consultation.Payment.TransactionId);

        if (gatewayStatus == PaymentGatewayStatus.Captured || gatewayStatus == PaymentGatewayStatus.Authorized)
        {
            consultation.Status = ConsultationStatus.Confirmed;
            consultation.Payment.Status = PaymentStatus.Succeeded;
            consultation.Payment.TransactionDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _notificationService.SendBookingConfirmedAsync(
                consultation.ClientId, consultation.LawyerId, consultation.ScheduledAt);

            _logger.LogInformation("Consultation {Id} confirmed via fallback worker.", consultationId);
        }
        else if (gatewayStatus == PaymentGatewayStatus.Failed)
        {
            consultation.Status = ConsultationStatus.Cancelled;
            consultation.Payment.Status = PaymentStatus.Failed;
            await _context.SaveChangesAsync();
            _logger.LogWarning("Consultation {Id} failed payment. Marked cancelled.", consultationId);
        }
        else
        {
            _logger.LogInformation("Consultation {Id} still pending in gateway.", consultationId);
        }
    }
}