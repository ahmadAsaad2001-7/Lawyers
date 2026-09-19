namespace Lawyers.Application.DTOs;


public class TopLawyerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Consultations { get; set; }
    public decimal AverageRating { get; set; }
}