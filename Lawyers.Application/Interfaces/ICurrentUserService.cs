using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Application.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsAuthenticated { get; }
    string? Email { get; } // Add this line
    bool IsAdmin { get; }   // ✅ ADD
    Roles? Role { get; }

}