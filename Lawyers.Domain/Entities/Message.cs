namespace Lawyers.Domain.Entities;

public class Message : AuditableEntity
{
    public string Content { get; set; } = string.Empty;
    
    public int ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
}