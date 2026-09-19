namespace Lawyers.Application.DTOs;

public class UserListDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Unified display name (profile's FullName if exists, else email)
    public string DisplayName { get; set; } = string.Empty;

    // ── Lawyer-specific (null if user is not a lawyer) ──
    public int? LawyerProfileId { get; set; }
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool? IsVerified { get; set; }
    public decimal? AverageRating { get; set; }
    public string? LawFirmName { get; set; }

    // ── Client-specific (null if user is not a client) ──
    public int? ClientProfileId { get; set; }
    public string? ClientFullName { get; set; }

    // ── Useful counters for the admin table ──
    public int ConsultationCount { get; set; }
    public int PostsCount { get; set; }
    public int FreeMessagesCount { get; set; }
}