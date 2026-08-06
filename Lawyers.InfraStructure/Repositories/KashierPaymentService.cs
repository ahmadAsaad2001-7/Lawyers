using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Lawyers.Application.DTOs;
using Lawyers.Application.Enums;
using Lawyers.Application.Interfaces;
using Lawyers.InfraStructure.Helpers;
using Microsoft.Extensions.Options;

namespace Lawyers.InfraStructure.Services;

public class KashierPaymentService : IPaymentService
{
    private readonly KashierOptions _options;

    public KashierPaymentService(HttpClient httpClient, IOptions<KashierOptions> options)
    {
        _options = options.Value;

        var baseUrl = _options.BaseUrl;
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }

        httpClient.BaseAddress = new Uri(baseUrl);
    }

    public Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(InitiatePaymentDto dto)
    {
        ValidateKashierSettings();

        var merchantOrderId = dto.ConsultationId.ToString(CultureInfo.InvariantCulture);
        var amount = dto.Amount.ToString("0.##", CultureInfo.InvariantCulture);
        var hashPath = $"/?payment={_options.MerchantId}.{merchantOrderId}.{amount}.{dto.Currency}";
        var hash = GenerateKashierHash(hashPath, _options.ApiKey);

        var queryParams = new Dictionary<string, string?>
        {
            ["merchantId"] = _options.MerchantId,
            ["orderId"] = merchantOrderId,
            ["amount"] = amount,
            ["currency"] = dto.Currency,
            ["mode"] = _options.Mode,
            ["hash"] = hash,
            ["merchantRedirect"] = _options.MerchantRedirectUrl,
            ["serverWebhook"] = _options.ServerWebhookUrl,
            ["allowedMethods"] = MapAllowedMethods(dto.Channel),
            ["display"] = "ar",
            ["redirectMethod"] = "post",
            ["customerEmail"] = dto.CustomerEmail,
            ["customerMobile"] = dto.WalletPhoneNumber,
            ["customerName"] = "Consultation Client"
        };

        var checkoutUrl = BuildUrl(_options.CheckoutUrl, queryParams);

        return Task.FromResult(new PaymentIntentResponseDto(
            PaymentIntentId: merchantOrderId,
            ClientSecret: checkoutUrl,
            Amount: dto.Amount,
            Currency: dto.Currency
        ));
    }

    public Task<bool> CapturePaymentAsync(string paymentIntentId) => Task.FromResult(true);

    public Task<bool> CancelPaymentAsync(string paymentIntentId) => Task.FromResult(true);

    public Task<PaymentGatewayStatus> GetPaymentStatusAsync(string transactionId)
    {
        return Task.FromResult(PaymentGatewayStatus.Pending);
    }

    private static string MapAllowedMethods(PaymentChannel channel)
    {
        return channel switch
        {
            PaymentChannel.Card => "card",
            PaymentChannel.MobileWallet => "wallet",
            PaymentChannel.InstaPay => "card,wallet",
            _ => "card,wallet"
        };
    }

    private static string GenerateKashierHash(string path, string apiKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string BuildUrl(string baseUrl, IReadOnlyDictionary<string, string?> queryParams)
    {
        var normalizedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://checkout.kashier.io/"
            : baseUrl;

        var separator = normalizedBaseUrl.Contains('?') ? '&' : '?';
        var query = string.Join("&", queryParams
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));

        return $"{normalizedBaseUrl}{separator}{query}";
    }

    private void ValidateKashierSettings()
    {
        if (string.IsNullOrWhiteSpace(_options.MerchantId))
        {
            throw new InvalidOperationException("Kashier MerchantId is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Kashier ApiKey is not configured.");
        }
    }
}
