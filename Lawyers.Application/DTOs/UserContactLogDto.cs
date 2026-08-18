namespace Lawyers.Application.DTOs;

public class UserContactLogDto
{
    public string ContactType { get; set; } = string.Empty; // "Consultation" or "Free Inquiry"
    public int RelatedEntityId { get; set; }                // ID of the Consultation or Message
    public DateTime Timestamp { get; set; }                 // When the contact was initiated
    
    public string InitiatorName { get; set; } = string.Empty;
    public string InitiatorRole { get; set; } = string.Empty;
    
    public string TargetName { get; set; } = string.Empty;
    public string TargetRole { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty; 
}