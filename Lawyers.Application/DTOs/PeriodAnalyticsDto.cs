namespace Lawyers.Application.DTOs;

public class PeriodAnalyticsDto
{
    public decimal PeriodRevenue { get; set; }
    public decimal QuarterRevenue { get; set; }
    public int ConsultationsCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal CompletionRate { get; set; }
    public int NewUsersCount { get; set; }
    public List<TopLawyerDto> TopLawyers { get; set; } = new();
}
