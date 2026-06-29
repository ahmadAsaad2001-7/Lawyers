namespace Lawyers.Application.DTOs;

public class LawyerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
}