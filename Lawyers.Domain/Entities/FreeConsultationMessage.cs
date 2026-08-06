namespace Lawyers.Domain.Entities;

public class FreeConsultationMessage
{
    public int Id { get; set; }
    public int LawyerId { get; set; }
    public LawyerProfile Lawyer { get; set; } // Navigation property

    public string SenderName { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    public string SenderIpAddress { get; set; } = string.Empty;
    public bool IsRepliedTo { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}