using System.Text.Json.Serialization;

namespace Lawyers.Application.DTOs;

public class LawyerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    [JsonPropertyName("avatar")] 
    public string? Avatar { get; set; } 
    public decimal HourlyRate { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public bool IsVerified { get; set; } 
    public string LawFirmName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}