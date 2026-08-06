using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using Lawyers.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lawyers.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService; // ✅ 1. Inject Email Service
    private IUnitOfWork _unitOfWork;

    public AuthService(UserManager<User> userManager, IConfiguration configuration, IEmailService emailService,IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

     public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
{
    var user = new User
    {
        UserName = request.Email,
        Email = request.Email,
        // ✅ Use the enum value directly (will store as integer)
        Role = request.Role  
    };

    var result = await _userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new ApplicationException($"Registration failed: {errors}");
    }

    // ✅ Create the profile based on the role
    if (request.Role == Roles.Client) // Client
    {
        await _unitOfWork.ClientProfiles.AddAsync(new ClientProfile
        {
            UserId = user.Id,
            FullName = request.FullName ?? user.Email,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id
        });
    }
    else if (request.Role == Roles.Lawyer) // Lawyer
    {
        await _unitOfWork.LawyerProfiles.AddAsync(new LawyerProfile
        {
            UserId = user.Id,
            FullName = request.FullName ?? user.Email,
            Bio = "New lawyer profile",
            HourlyRate = 100.00m,
            Specialization = "General",
            Address = new Address
            {
                Street = request.Address?.Street ?? "",
                City = request.Address?.City ?? "",
                State = request.Address?.State ?? "",
                Country = request.Address?.Country ?? "",
                PostalCode = request.Address?.PostalCode ?? ""
            },
            BarLicenseNumber = "PENDING",
            IsVerified = false,
            AverageRating = 0.0m,
            LawFirmName = request.LawFirmName ?? "",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id
        });
    }

    await _unitOfWork.SaveChangesAsync();

    // Send verification email...
    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
    var frontendUrl = _configuration["FrontendUrl"] ?? "https://localhost:3000";
    var verificationLink = $"{frontendUrl}/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
    await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

    return new AuthResponse { Token = null, Email = user.Email, Role = user.Role };
}

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new UnauthorizedAccessException("Invalid email or password.");

        // ✅ 4. BLOCK LOGIN IF EMAIL IS NOT VERIFIED
        var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        if (!isEmailConfirmed)
        {
            throw new UnauthorizedAccessException("Please verify your email address before logging in. Check your inbox or spam folder.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid) throw new UnauthorizedAccessException("Invalid email or password.");

        return await GenerateToken(user);
    }
    public async Task<bool> ForgotPasswordAsync(string email, string frontendUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
    
        // 🔒 SECURITY: Always return true, even if the email doesn't exist. 
        // This prevents attackers from "enumerating" (guessing) which emails are registered.
        if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            return true; 
        }

        // Generate the secure reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
    
        // Create the link for the frontend
        var resetLink = $"{frontendUrl}/auth/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        // Send the email
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new ApplicationException("Invalid password reset request.");
        }

        // Identity handles the token validation and password hashing automatically
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
    
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApplicationException($"Password reset failed: {errors}");
        }

        return true;
    }
    private async Task<AuthResponse> GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        if (!double.TryParse(jwtSettings["ExpirationInMinutes"], out var expirationMinutes))
        {
            expirationMinutes = 60;
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new AuthResponse
        {
            UserId = user.Id,                                    // ✅ Added
            UserName = user.UserName ?? user.Email,              // ✅ Added (Fallback to email)
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            Role = user.Role,
        };
    }
}