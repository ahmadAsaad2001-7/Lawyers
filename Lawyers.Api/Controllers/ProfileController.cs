using Lawyers.Application.Features.Lawyers.Commands;
using Lawyers.Application.Features.Profile.Queries;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }
 
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            return Unauthorized();

        var overview = await _mediator.Send(new GetProfileOverviewQuery(_currentUser.UserId.Value));
        return Ok(overview);
    }
    
    [Authorize]
    [HttpPatch("lawyer-profile")]
    public async Task<IActionResult> CompleteLawyerProfile([FromBody] CompleteLawyerProfileCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Profile updated. You can now apply for verification." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize]
    [HttpPost("apply-verification")]
    public async Task<IActionResult> ApplyForVerification([FromBody] ApplyForVerificationCommand command)
    {
        try
        {
            var voteId = await _mediator.Send(command);
            return Ok(new { message = "Your application has been submitted for admin review.", voteId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}