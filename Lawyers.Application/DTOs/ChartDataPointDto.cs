namespace Lawyers.Application.DTOs;

public class ChartDataPointDto
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty; // e.g., "Mon", "12 Aug"
    public decimal Revenue { get; set; }
    public int Consultations { get; set; }
}