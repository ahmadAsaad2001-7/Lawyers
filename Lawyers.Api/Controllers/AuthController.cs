using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lawyers.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
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

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        
        return Ok(new { userId, email, message = "Authentication successful!" });
    }
}