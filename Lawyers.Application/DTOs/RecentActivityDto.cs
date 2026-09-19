namespace Lawyers.Application.DTOs;

public class RecentActivityDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string LawyerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}