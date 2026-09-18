namespace Lawyers.Domain.Entities.Enums;

public class PlatformNotification
{
    public int RecipientUserId { get; set; }
    public User Recipient { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
}