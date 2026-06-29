namespace Lawyers.Domain.Entities;

public class ClientProfile : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}