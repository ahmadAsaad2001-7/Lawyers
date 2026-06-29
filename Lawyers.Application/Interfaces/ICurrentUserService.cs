namespace Lawyers.Application.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsAuthenticated { get; }
    string? Email { get; } // Add this line
}