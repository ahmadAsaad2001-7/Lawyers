using Lawyers.Domain.ValueObjects;

namespace Lawyers.Domain.Entities;

public class LawyerProfile : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    
    public Address Address { get; set; } = null!;
    
    public string BarLicenseNumber { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    
    public string Specialization { get; set; } = string.Empty; // e.g., "Family Law", "Corporate"
    public decimal AverageRating { get; set; } = 0.0m;
}