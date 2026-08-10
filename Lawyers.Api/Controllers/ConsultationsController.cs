using Lawyers.Application.DTOs;
using Lawyers.Application.Features.Consultations.Commands;
using Lawyers.Application.Features.Consultations.Queries;
using Lawyers.Application.Features.Payments.Commands;
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
    private readonly IWebHostEnvironment _env; // ✅ injected

    public ConsultationsController(IMediator mediator,IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    [HttpPost("book")]
    public async Task<ActionResult<BookingResponseDto>> BookConsultation([FromBody] BookConsultationCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            // 🚧🚧 DEVELOPMENT-ONLY BYPASS 🚧🚧
            // Simulates a successful Kashier webhook so the consultation is
            // confirmed instantly and the chat unlocks without a tunnel/webhook.
            // In Production this block NEVER runs (env != Development),
            // so the real webhook remains the only path to confirmation.
            if (_env.IsDevelopment() && result != null)
            {
                try
                {
                    await _mediator.Send(new NotifyPaymentSuccessCommand
                    {
                        ConsultationId = result.ConsultationId,
                        GatewayPaymentId = $"DEV-AUTO-{result.ConsultationId}",
                        Status = "SUCCESS"
                    });
                    Console.WriteLine($"[DEV-BYPASS] Consultation {result.ConsultationId} auto-confirmed (no webhook needed).");
                }
                catch (Exception devEx)
                {
                    // Never let the bypass break a real booking
                    Console.WriteLine($"[DEV-BYPASS] Failed (ignored): {devEx.Message}");
                }
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
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
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
        
    // Add this inside your ConsultationsController class:

    [HttpPost("free-messages/{id}/reply")]
    public async Task<IActionResult> ReplyToFreeMessage(int id, [FromBody] ReplyToFreeMessageCommand command)
    {
        try
        {
            command.MessageId = id;
            await _mediator.Send(command);
            return Ok(new { message = "Reply sent successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Reply Error]: {ex.Message}");
            return StatusCode(500, new { message = "Failed to send reply" });
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

    [HttpGet("my-consultations")]
    public async Task<IActionResult> GetMyConsultations()
    {
        try
        {
            var query = new GetMyConsultationsQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetMyConsultations Error]: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while fetching your consultations" });
        }
    }
}