using Lawyers.Application.DTOs;
using Lawyers.Application.Features.Consultations.Commands;
using Lawyers.Application.Features.Consultations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConsultationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("book")]
    public async Task<ActionResult<BookingResponseDto>> BookConsultation([FromBody] BookConsultationCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[Booking Error - Unauthorized]: {ex.Message}");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // 🔴 THIS WILL PRINT THE EXACT FAILURE IN RIDER
            Console.WriteLine($"[Booking Error Exception]: {ex}");
            return BadRequest(new { message = ex.Message });
        }
    }
    
    
    [HttpGet("free-messages")]
    public async Task<ActionResult<List<FreeMessageDto>>> GetFreeMessages()
    {
        try
        {
            var query = new GetLawyerFreeMessagesQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetFreeMessages Error]: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while fetching your messages" });
        }
    }

    [HttpPost("free-message")]
    [AllowAnonymous]
    public async Task<IActionResult> SendFreeMessage([FromBody] SendFreeMessageCommand command)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress.ToString() ?? "unknown";
            command.IpAddress = ipAddress;
            await _mediator.Send(command);
            return Ok(new { message = "message sent successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FreeMessage Error]: {ex.Message}]");
            return StatusCode(500, new { message = "an error occured while sending your message" });
        }

    }
    
    

    // ✅ NEW: Get Consultation Details for Chat UI
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetConsultationDetails(int id)
    {
        try
        {
            var query = new ConsultationDetailsQuery(id);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Optional: Get all consultations for the current user (for the sidebar)
    [HttpGet("my-consultations")]
    public async Task<IActionResult> GetMyConsultations()
    {
        // You can create another MediatR query for this
        // For now, this is a placeholder
        return Ok(new { message = "Implement ConsultationListQuery here" });
    }
}