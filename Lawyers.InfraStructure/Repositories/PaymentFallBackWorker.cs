using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using Lawyers.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lawyers.Infrastructure.Services;

public class PaymentFallbackWorker
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService; // Inject the abstraction!
    private readonly ILogger<PaymentFallbackWorker> _logger;

    public PaymentFallbackWorker(AppDbContext context, IPaymentService paymentService, ILogger<PaymentFallbackWorker> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    // This is the method Hangfire will execute
    public async Task CheckAndConfirmPaymentStatusAsync(int consultationId)
    {
        _logger.LogInformation("Background Worker: Checking payment status for Consultation {Id}", consultationId);

        var consultation = await _context.Consultations
            .Include(c => c.Payment)
            .FirstOrDefaultAsync(c => c.Id == consultationId);

        if (consultation?.Payment == null || consultation.Status != ConsultationStatus.Pending)
        {
            _logger.LogWarning("Consultation {Id} not found or already processed.", consultationId);
            return;
        }

        // 1. Ask the Payment Gateway (Kashier) what the actual status is
        var gatewayStatus = await _paymentService.GetPaymentStatusAsync(consultation.Payment.StripePaymentIntentId); 
        // Note: You might want to rename StripePaymentIntentId to TransactionId in your Payment entity now!

        // 2. Act based on the gateway's response
        if (gatewayStatus == PaymentGatewayStatus.Captured || gatewayStatus == PaymentGatewayStatus.Authorized)
        {
            consultation.Status = ConsultationStatus.Confirmed;
            consultation.Payment.Status = PaymentStatus.Succeeded;
            consultation.Payment.TransactionDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Consultation {Id} successfully confirmed via Background Worker!", consultationId);
        }
        else if (gatewayStatus == PaymentGatewayStatus.Failed)
        {
            consultation.Status = ConsultationStatus.Cancelled;
            consultation.Payment.Status = PaymentStatus.Failed;
            await _context.SaveChangesAsync();
            _logger.LogWarning("Consultation {Id} failed payment in gateway. Marked as cancelled.", consultationId);
        }
        else
        {
            _logger.LogInformation("Consultation {Id} is still pending in gateway. Will check again later.", consultationId);
            // Optional: Reschedule the job to check again in 10 minutes using Hangfire
        }
    }
}