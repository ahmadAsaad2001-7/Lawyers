using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Domain.Entities;

public class Consultation : AuditableEntity
{
    public int ClientId { get; set; }
    public ClientProfile Client { get; set; } = null!;
    
    public int LawyerId { get; set; }
    public LawyerProfile Lawyer { get; set; } = null!;
    
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    
    public ConsultationStatus Status { get; set; } = ConsultationStatus.Pending;
    
    // Business logic method (OOP Encapsulation)
    public void MarkAsConfirmed()
    {
        if (Status != ConsultationStatus.Pending)
            throw new InvalidOperationException("Only pending consultations can be confirmed.");
        
        Status = ConsultationStatus.Confirmed;
    }
    
    public void MarkAsCompleted()
    {
        if (Status != ConsultationStatus.InProgress)
            throw new InvalidOperationException("Only in-progress consultations can be completed.");
        
        Status = ConsultationStatus.Completed;
    }
    
    public void Cancel()
    {
        if (Status == ConsultationStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed consultation.");
        
        Status = ConsultationStatus.Cancelled;
    }
}