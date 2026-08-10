using Lawyers.Domain.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace Lawyers.Domain.Entities;

// Change BaseEntity to IdentityUser<int>
public class User : IdentityUser<int>
{
    
    public Roles Role { get; set; }
    public string ProfileImageUrl { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public byte[] RowVersion { get; set; } = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }; 
    
    // Navigation properties
    public ClientProfile? ClientProfile { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }
}