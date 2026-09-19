using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly UserManager<User> _userManager;  
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, UserManager<User> userManager,IConfiguration configuration)
    {
        _mediator = mediator;
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] int userId, [FromQuery] string token)
    {
        try
        {
            var command = new ConfirmEmailCommand(userId, token);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult GoogleLogin([FromQuery] string? role = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { role }); // ✅ forward role through the round trip
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? role = null) 
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "https://localhost:3000";

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded || result.Principal == null)
            return Redirect($"{frontendUrl}/auth/login?error=google_failed");

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name);
        var picture = result.Principal.FindFirstValue("picture");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (string.IsNullOrEmpty(email))
            return Redirect($"{frontendUrl}/auth/login?error=no_email");

        Roles? parsedRole = Enum.TryParse<Roles>(role, ignoreCase: true, out var r) ? r : null;

        try
        {
            var response = await _mediator.Send(new ExternalLoginCommand(email, name ?? email, picture, parsedRole));
            return Redirect($"{frontendUrl}/auth/google-callback?token={Uri.EscapeDataString(response.Token!)}");
        }
        catch
        {
            return Redirect($"{frontendUrl}/auth/login?error=google_login_failed");
        }
    }
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Re-issues a JWT for the currently authenticated user, reflecting their
    /// current role/verification state. Call this after a "you've been verified"
    /// notification instead of forcing a full re-login.
    /// </summary>
    [Authorize] // any authenticated user, regardless of role/verification status
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var response = await _mediator.Send(new RefreshTokenCommand());
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();

        var appUser = await _userManager.Users
            .Include(u => u.ClientProfile)
            .Include(u => u.LawyerProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id.ToString() == userIdClaim);

        if (appUser == null) return Unauthorized();

        return Ok(new
        {
            userId = appUser.Id,
            userName = appUser.UserName,
            email = appUser.Email,
            role = appUser.Role.ToString(),
            // ✅ NEW: prefer lawyer name, then client name, then username
            fullName = appUser.LawyerProfile?.FullName
                       ?? appUser.ClientProfile?.FullName
                       ?? appUser.UserName,
            profileImageUrl = appUser.ProfileImageUrl
        });
    }
}
