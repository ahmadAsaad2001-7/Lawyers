using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.InfraStructure.Helpers;
using Microsoft.Extensions.Options;

namespace Lawyers.InfraStructure.Services; // ✅ Moved to Services namespace

public class KashierPaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly KashierOptions _options;

    public KashierPaymentService(HttpClient httpClient, IOptions<KashierOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        
        // ⚠️ SENIOR TWEAK: Check Kashier docs. 
        // If they expect "Authorization: Bearer YOUR_KEY", use the two-parameter constructor.
        // If they expect "Authorization: YOUR_KEY", use the single-parameter constructor.
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey); 
    }

    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
        decimal amount, string currency, int consultationId, string customerEmail)
    {
        // ⚠️ SENIOR TWEAK: Double-check Kashier's docs! 
        // Stripe requires cents (amount * 100). Some MENA gateways require exact decimals (150.50).
        // If your test payments fail with "Invalid Amount", change this to just: Amount = amount
        long amountInPiastres = (long)(amount * 100);

        var payload = new KashierOrderRequest
        {
            MerchantId = _options.MerchantId,
            Amount = amountInPiastres,
            Currency = currency,
            MerchantOrderId = consultationId.ToString(), // Crucial for Webhooks/Background jobs!
            CustomerEmail = customerEmail,
            Operation = "Authorize" // Escrow hold
        };

        var response = await _httpClient.PostAsJsonAsync("orders", payload);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new ApplicationException($"Kashier Order creation failed: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<KashierOrderResponse>();

        // ✅ SENIOR TWEAK: Safe null checking instead of using the dangerous '!' operator
        if (result == null)
        {
            throw new ApplicationException("Kashier returned a success status, but the response body was empty or malformed.");
        }

        return new PaymentIntentResponseDto(
            PaymentIntentId: result.OrderId,
            ClientSecret: result.PaymentToken, // Note: 'ClientSecret' is a Stripe term, but fine to use here as a generic token holder
            Amount: amount,
            Currency: currency
        );
    }

    public async Task<bool> CapturePaymentAsync(string paymentIntentId)
    {
        var response = await _httpClient.PostAsJsonAsync($"transactions/{paymentIntentId}/capture", new { });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelPaymentAsync(string paymentIntentId)
    {
        var response = await _httpClient.PostAsJsonAsync($"transactions/{paymentIntentId}/void", new { });
        return response.IsSuccessStatusCode;
    }

    // ✅ THE MISSING PIECE: Required for the Hangfire Background Worker
    public async Task<PaymentGatewayStatus> GetPaymentStatusAsync(string transactionId)
    {
        var response = await _httpClient.GetAsync($"transactions/{transactionId}");
        
        if (!response.IsSuccessStatusCode)
        {
            return PaymentGatewayStatus.Failed; // Or throw an exception depending on your preference
        }

        var result = await response.Content.ReadFromJsonAsync<KashierTransactionStatusResponse>();

        if (result == null) return PaymentGatewayStatus.Pending;

        // Map Kashier's specific string statuses to our clean, provider-agnostic enum
        return result.Status?.ToUpper() switch
        {
            "SUCCESS" or "CAPTURED" => PaymentGatewayStatus.Captured,
            "AUTHORIZED" or "PENDING" => PaymentGatewayStatus.Authorized,
            "FAILED" or "DECLINED" => PaymentGatewayStatus.Failed,
            "VOIDED" or "REFUNDED" => PaymentGatewayStatus.Voided,
            _ => PaymentGatewayStatus.Pending
        };
    }
}

#region Kashier Internal Models
internal class KashierOrderRequest
{
    [JsonPropertyName("merchantId")] public string MerchantId { get; set; } = string.Empty;
    [JsonPropertyName("amount")] public long Amount { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "EGP";
    [JsonPropertyName("merchantOrderId")] public string MerchantOrderId { get; set; } = string.Empty;
    [JsonPropertyName("customerEmail")] public string CustomerEmail { get; set; } = string.Empty;
    [JsonPropertyName("operation")] public string Operation { get; set; } = "Authorize";
}

internal class KashierOrderResponse
{
    [JsonPropertyName("orderId")] public string OrderId { get; set; } = string.Empty;
    [JsonPropertyName("paymentToken")] public string PaymentToken { get; set; } = string.Empty;
}

// ✅ Added for the Background Worker
internal class KashierTransactionStatusResponse
{
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("amount")] public decimal? Amount { get; set; }
}
#endregion