using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace Lawyers.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<User> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Role = request.Role
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApplicationException($"Registration failed: {errors}"); // Or custom Domain Exception
        }

        try
        {
            await _userManager.AddToRoleAsync(user, request.Role.ToString());
        }
        catch
        {
            // Rollback user creation if role assignment fails to avoid orphaned accounts
            await _userManager.DeleteAsync(user);
            throw new ApplicationException("Registration failed during role assignment.");
        }

        return await GenerateToken(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new UnauthorizedAccessException("Invalid email or password.");

        // Check password using ONLY UserManager
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid) throw new UnauthorizedAccessException("Invalid email or password.");

        return await GenerateToken(user);
    }

    private async Task<AuthResponse> GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is missing from configuration.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)); // Using UTF8 instead of ASCII
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        if (!double.TryParse(jwtSettings["ExpirationInMinutes"], out var expirationMinutes))
        {
            expirationMinutes = 60; // 1-hour safe fallback
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
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            Role = user.Role
        };
    }
}