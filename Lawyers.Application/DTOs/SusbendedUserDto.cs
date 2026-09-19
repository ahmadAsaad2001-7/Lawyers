namespace Lawyers.Application.Features.DTOs;

public class SuspendedUserDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int RemainingDays { get; set; }
}