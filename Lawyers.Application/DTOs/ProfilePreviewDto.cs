
namespace Lawyers.Application.DTOs;

public class ProfileOverviewDto
{
    public int TotalConsultations { get; set; }
    public decimal TotalEarned { get; set; }
    public double AverageRating { get; set; }
    public DateTime MemberSince { get; set; }
    public List<UpcomingConsultationDto> UpcomingConsultations { get; set; } = new();
    public List<SimpleActivityDto> RecentActivities { get; set; } = new();
}

public class UpcomingConsultationDto
{
    public int Id { get; set; }
    public string OtherPartyName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SimpleActivityDto
{
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}