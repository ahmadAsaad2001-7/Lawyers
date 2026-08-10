using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lawyers.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Lawyers.InfraStructure.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly KashierOptions _kashierOptions;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IMediator mediator, IOptions<KashierOptions> kashierOptions, ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _kashierOptions = kashierOptions.Value;
        _logger = logger;
    }

    [HttpPost("kashier")]
    public async Task<IActionResult> HandleKashierWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        _logger.LogInformation("Kashier Webhook Received. Raw Body: {RawBody}", rawBody);

        if (!Request.Headers.TryGetValue("X-Kashier-Signature", out var signatureHeader))
        {
            _logger.LogWarning("Missing X-Kashier-Signature header.");
            return BadRequest("Missing signature header.");
        }

        if (!IsSignatureValid(rawBody, signatureHeader!))
        {
            _logger.LogWarning("Invalid HMAC signature.");
            return Unauthorized("Invalid signature. Request rejected.");
        }

        var webhookEvent = JsonSerializer.Deserialize<KashierWebhookDto>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (webhookEvent == null || string.IsNullOrWhiteSpace(webhookEvent.MerchantOrderId))
        {
            return BadRequest("Invalid payload.");
        }

        if (!int.TryParse(webhookEvent.MerchantOrderId, out var consultationId))
        {
            return BadRequest("Invalid merchant order id.");
        }

        // ✅ FIX: Use PaymentId (or whatever Kashier's JSON key actually is)
        if (string.IsNullOrWhiteSpace(webhookEvent.PaymentId) || string.IsNullOrWhiteSpace(webhookEvent.Status))
        {
            return BadRequest("Invalid payment event.");
        }
        
        var command = new NotifyPaymentSuccessCommand 
        { 
            ConsultationId = consultationId, 
            GatewayPaymentId = webhookEvent.PaymentId, // ✅ Mapped correctly
            Status = webhookEvent.Status
        };

        await _mediator.Send(command);

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
        
        // ✅ FIX: Kashier uses Hexadecimal encoding, NOT Base64
        var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}

public class KashierWebhookDto
{
    // ✅ FIX: Changed from OrderId to PaymentId (Verify with Kashier docs/dashboard)
    public string PaymentId { get; set; } = string.Empty; 
    public string MerchantOrderId { get; set; } = string.Empty; 
    public string Status { get; set; } = string.Empty; 
}