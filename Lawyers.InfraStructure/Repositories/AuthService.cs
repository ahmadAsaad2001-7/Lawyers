using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using Lawyers.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lawyers.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(UserManager<User> userManager, IConfiguration configuration, IEmailService emailService, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

   public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
{
    // ✅ Lawyers don't get the real Lawyer role until an admin vote approves them.
    var initialRole = request.Role == Roles.Lawyer ? Roles.PendingLawyer : request.Role;

    var user = new User
    {
        UserName = request.Email,
        Email = request.Email,
        Role = initialRole
    };

    var result = await _userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new ApplicationException($"Registration failed: {errors}");
    }

    if (initialRole == Roles.Client)
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
    else if (initialRole == Roles.PendingLawyer)
    {
        await _unitOfWork.LawyerProfiles.AddAsync(new LawyerProfile
        {
            UserId = user.Id,
            FullName = request.FullName ?? user.Email,
            Bio = request.Bio ?? "New lawyer profile",
            HourlyRate = request.HourlyRate ?? 100.00m,
            Specialization = request.Specialization ?? "General",
            Address = new Address
            {
                Street = request.Address?.Street ?? "",
                City = request.Address?.City ?? "",
                State = request.Address?.State ?? "",
                Country = request.Address?.Country ?? "",
                PostalCode = request.Address?.PostalCode ?? ""
            },
            // ✅ actually store what they submitted instead of hardcoding "PENDING" —
            // admins need something real to look at when they vote.
            BarLicenseNumber = request.BarLicenseNumber ?? "",
            IsVerified = false,
            AverageRating = 0.0m,
            LawFirmName = request.LawFirmName ?? "",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id
        });
    }

    await _unitOfWork.SaveChangesAsync();

    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
    var frontendUrl = _configuration["FrontendUrl"] ?? "https://localhost:3000";
    var verificationLink = $"{frontendUrl}/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
    await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

    return new AuthResponse { Token = null, Email = user.Email, Role = user.Role };
}

    // ✅ FIXED: ExternalLoginAsync
    public async Task<AuthResponseDto> ExternalLoginAsync(string email, string name, string? pictureUrl, Roles? role)    {
        var user = await _userManager.FindByEmailAsync(email);

        // FIX 1: Changed 'user == 0' to 'user == null'
        if (user == null)
        {
            var assignedRole = role == Roles.Lawyer ? Roles.PendingLawyer : (role ?? Roles.Client);

            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Role = assignedRole,
                ProfileImageUrl = pictureUrl
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new Exception("Failed to create user from Google account.");

            if (assignedRole == Roles.Client)
            {
                await _unitOfWork.ClientProfiles.AddAsync(new ClientProfile
                {
                    UserId = user.Id, FullName = name, PhoneNumber = string.Empty,
                    IsDeleted = false, CreatedAt = DateTime.UtcNow, CreatedByUserId = user.Id
                });
            }
            else if (assignedRole == Roles.PendingLawyer)
            {
                await _unitOfWork.LawyerProfiles.AddAsync(new LawyerProfile
                {
                    UserId = user.Id,
                    FullName = name,
                    Bio = "New lawyer profile",
                    HourlyRate = 100.00m,
                    Specialization = "General",
                    Address = new Address { Street = "", City = "", State = "", Country = "", PostalCode = "" },
                    BarLicenseNumber = "", // empty, not "PENDING" — matches your RegisterAsync convention now
                    IsVerified = false,
                    AverageRating = 0.0m,
                    LawFirmName = "",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = user.Id
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            var needsUpdate = false;

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                needsUpdate = true;
            }

            if (!string.IsNullOrEmpty(pictureUrl) && user.ProfileImageUrl != pictureUrl)
            {
                user.ProfileImageUrl = pictureUrl; // ✅ keep avatar in sync on repeat logins
                needsUpdate = true;
            }

            if (needsUpdate)
                await _userManager.UpdateAsync(user);
        }

        var userWithProfiles = await _userManager.Users
            .Include(u => u.LawyerProfile)
            .Include(u => u.ClientProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        bool isPlatformVerified = true;
        string? fullName = name;

        if (userWithProfiles?.LawyerProfile != null)
        {
            isPlatformVerified = userWithProfiles.LawyerProfile.IsVerified;
            fullName = userWithProfiles.LawyerProfile.FullName;
        }
        else if (userWithProfiles?.ClientProfile != null)
        {
            fullName = userWithProfiles.ClientProfile.FullName;
        }

        var authResult = await GenerateToken(user, isPlatformVerified, fullName, user.ProfileImageUrl);

        return new AuthResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            Token = authResult.Token,
            Email = user.Email,
            Role = user.Role.ToString(),
            FullName = fullName,
            ProfileImageUrl = user.ProfileImageUrl,
            IsPlatformVerified = isPlatformVerified,
            Message = "Google login successful."
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new UnauthorizedAccessException("Invalid email or password.");

        var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        if (!isEmailConfirmed)
            throw new UnauthorizedAccessException("Please verify your relationship before logging in.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid) throw new UnauthorizedAccessException("Invalid email or password.");

        var userWithProfiles = await _userManager.Users
            .Include(u => u.LawyerProfile)
            .Include(u => u.ClientProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        bool isPlatformVerified = true;
        string? fullName = user.UserName;
        string? profileImage = user.ProfileImageUrl; // Note: Ensure this exists on User, otherwise remove

        if (userWithProfiles?.LawyerProfile != null)
        {
            isPlatformVerified = userWithProfiles.LawyerProfile.IsVerified;
            fullName = userWithProfiles.LawyerProfile.FullName;
        }
        else if (userWithProfiles?.ClientProfile != null)
        {
            fullName = userWithProfiles.ClientProfile.FullName;
        }

        return await GenerateToken(user, isPlatformVerified, fullName, profileImage);
    }

    public async Task<bool> ForgotPasswordAsync(string email, string frontendUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
    
        if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            return true; 
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"{frontendUrl}/auth/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

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

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
    
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApplicationException($"Password reset failed: {errors}");
        }

        return true;
    }
    public async Task<AuthResponse> RefreshTokenAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new UnauthorizedAccessException("User not found.");

        if (user.IsDeleted)
            throw new UnauthorizedAccessException("This account is no longer active.");

        var userWithProfiles = await _userManager.Users
            .Include(u => u.LawyerProfile)
            .Include(u => u.ClientProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        bool isPlatformVerified = true;
        string? fullName = user.UserName;
        string? profileImage = user.ProfileImageUrl;

        if (userWithProfiles?.LawyerProfile != null)
        {
            isPlatformVerified = userWithProfiles.LawyerProfile.IsVerified;
            fullName = userWithProfiles.LawyerProfile.FullName;
        }
        else if (userWithProfiles?.ClientProfile != null)
        {
            fullName = userWithProfiles.ClientProfile.FullName;
        }


        return await GenerateToken(user, isPlatformVerified, fullName, profileImage);
    }

    private async Task<AuthResponse> GenerateToken(User user, bool isPlatformVerified, string? fullName, string? profileImage)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("IsPlatformVerified", isPlatformVerified.ToString().ToLower())
        };

        if (!double.TryParse(jwtSettings["ExpirationInMinutes"], out var expirationMinutes))
            expirationMinutes = 60;

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new AuthResponse
        {
            UserId = user.Id,
            UserName = user.UserName ?? user.Email,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            Role = user.Role,
            FullName = fullName,
            ProfileImageUrl = profileImage,
            IsPlatformVerified = isPlatformVerified
        };
    }
}