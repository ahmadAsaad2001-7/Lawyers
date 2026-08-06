using Lawyers.Domain.Entities.Enums;
using Lawyers.Domain.ValueObjects;

namespace Lawyers.Application.Interfaces;

// Clean, immutable positional records
public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string FullName, Roles Role, string LawFirmName,Address Address,string PhoneNumber);

public class AuthResponse
{ 
    public int UserId { get; set; }          
    public string UserName { get; set; } = string.Empty; 
    public string Token { get; set; }
    public string Email { get; set; }
    public Domain.Entities.Enums.Roles Role { get; set; }
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> ForgotPasswordAsync(string email, string frontendUrl);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}