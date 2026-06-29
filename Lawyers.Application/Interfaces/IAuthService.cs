using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Application.Interfaces;

// Clean, immutable positional records
public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string FullName, Roles Role);

public class AuthResponse
{
    public string Token { get; set; }
    public string Email { get; set; }
    public Domain.Entities.Enums.Roles Role { get; set; }
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}