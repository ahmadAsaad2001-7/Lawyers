using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lawyers.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Lawyers.InfraStructure.Helpers; // Where your KashierOptions live

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly KashierOptions _kashierOptions;

    public WebhooksController(IMediator mediator, IOptions<KashierOptions> kashierOptions)
    {
        _mediator = mediator;
        _kashierOptions = kashierOptions.Value;
    }

    [HttpPost("kashier")]
    public async Task<IActionResult> HandleKashierWebhook()
    {
        // 1. Read the raw request body (Required for HMAC validation)
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        // 2. Get the signature Kashier sent in the headers
        if (!Request.Headers.TryGetValue("X-Kashier-Signature", out var signatureHeader))
        {
            return BadRequest("Missing signature header.");
        }

        // 3. Validate the HMAC (Ensure the request actually came from Kashier)
        if (!IsSignatureValid(rawBody, signatureHeader!))
        {
            return Unauthorized("Invalid signature. Request rejected.");
        }

        // 4. Parse the JSON into a DTO
        var webhookEvent = JsonSerializer.Deserialize<KashierWebhookDto>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (webhookEvent == null || string.IsNullOrWhiteSpace(webhookEvent.MerchantOrderId))
        {
            return BadRequest("Invalid payload.");
        }

        // 5. Send to MediatR to update the database
        // (We convert MerchantOrderId back to int, since we mapped consultationId to it!)
        if (!int.TryParse(webhookEvent.MerchantOrderId, out var consultationId))
        {
            return BadRequest("Invalid merchant order id.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.OrderId) || string.IsNullOrWhiteSpace(webhookEvent.Status))
        {
            return BadRequest("Invalid payment event.");
        }
        
        var command = new NotifyPaymentSuccessCommand 
        { 
            ConsultationId = consultationId, 
            TransactionId = webhookEvent.OrderId,
            Status = webhookEvent.Status
        };

        await _mediator.Send(command);

        // 6. Always return 200 OK to Kashier quickly, otherwise they will retry!
        return Ok();
    }

    private bool IsSignatureValid(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(_kashierOptions.SecretKey))
            throw new InvalidOperationException("Kashier webhook secret is not configured.");

        if (string.IsNullOrWhiteSpace(signature))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_kashierOptions.SecretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature));
    }
}

// Internal DTO for the Webhook payload
public class KashierWebhookDto
{
    public string OrderId { get; set; } = string.Empty;
    public string MerchantOrderId { get; set; } = string.Empty; // This is our ConsultationId
    public string Status { get; set; } = string.Empty; // e.g., "SUCCESS"
}
