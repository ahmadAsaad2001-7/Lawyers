namespace Lawyers.Application.DTOs;

public class FreeMessageDto
{
    public int Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRepliedTo { get; set; }  
}