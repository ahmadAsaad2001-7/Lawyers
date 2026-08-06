namespace Lawyers.InfraStructure.Helpers;
public class KashierOptions
{
    public const string SectionName = "KashierSettings";
    public string MerchantId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://test-api.kashier.io/";
    public string CheckoutUrl { get; set; } = "https://checkout.kashier.io/";
    public string Mode { get; set; } = "test";
    public string MerchantRedirectUrl { get; set; } = "http://localhost:3000/consultations";
    public string ServerWebhookUrl { get; set; } = "https://localhost:7129/api/webhooks/kashier";
}
