using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Domain.Entities;

public class Payment : AuditableEntity
{
    
    public int ClientId { get; set; }
    public ClientProfile Client { get; set; } = null!;
    
    public int LawyerId { get; set; }
    public LawyerProfile Lawyer { get; set; } = null!;
    
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public int ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    // Nullable because it only gets a date when the payment actually succeeds
    public DateTime? TransactionDate { get; set; }
    
    // Business logic
    public void MarkAsSucceeded()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be marked as succeeded.");
        
        Status = PaymentStatus.Succeeded;
        TransactionDate = DateTime.UtcNow;
    }
    
    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
    }
}