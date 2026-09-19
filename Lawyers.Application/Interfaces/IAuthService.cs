using Lawyers.Application.DTOs.Auth;
using Lawyers.Domain.Entities.Enums;
using Lawyers.Domain.ValueObjects;

namespace Lawyers.Application.Interfaces;

// Clean, immutable positional records
public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string Email, 
    string Password, 
    string FullName, 
    Roles Role, 
    string? LawFirmName,
    Address Address,
    string PhoneNumber,
    string? BarLicenseNumber = null,
    string? Specialization = null,
    string? Bio = null,
    decimal? HourlyRate = null
);
public class AuthResponse
{ 
    public int UserId { get; set; }          
    public string UserName { get; set; } = string.Empty; 
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Roles Role { get; set; }
    public string? FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsPlatformVerified { get; set; } = true; // Default true for Clients/Admins
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> ForgotPasswordAsync(string email, string frontendUrl);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    Task<AuthResponseDto> ExternalLoginAsync(string email, string name, string? pictureUrl,Roles? role);
    
    Task<AuthResponse> RefreshTokenAsync(int userId); // ✅ new
}