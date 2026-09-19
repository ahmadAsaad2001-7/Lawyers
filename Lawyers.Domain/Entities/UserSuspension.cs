namespace Lawyers.Domain.Entities;

public class UserSuspension : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}