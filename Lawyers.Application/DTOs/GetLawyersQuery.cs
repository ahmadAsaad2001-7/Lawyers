namespace Lawyers.Application.DTOs;

public class GetLawyersQuery
{
    public string? City { get; set; }
    public string? Specialization { get; set; }
    public decimal? MaxHourlyRate { get; set; }
    
    // Pagination defaults
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Sorting defaults
    public string SortBy { get; set; } = "HourlyRate"; 
    public bool IsDescending { get; set; } = false;
}