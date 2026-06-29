using Lawyers.Application.DTOs;
using Lawyers.Application.Features.Consultations.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Strict RBAC rule: Enforce authentication globally on this resource
public class ConsultationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConsultationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> BookConsultation([FromBody] BookConsultationCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            
            // Return 200 OK along with the token needed to open the Kashier checkout screen
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Note: On Week 6 you will wire up an Exception Middleware to clean this up globally.
            // For now, this securely relays your custom validation/overlapping business rule errors.
            return BadRequest(new { message = ex.Message });
        }
    }
}