namespace Lawyers.Application.DTOs;

public class DashBoardStatsDto
{
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveConsultations { get; set; }
    public int PendingVerifications { get; set; } 
}