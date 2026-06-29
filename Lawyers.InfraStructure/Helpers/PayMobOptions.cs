namespace Lawyers.InfraStructure.Helpers;

public class PaymobOptions
{
    public const string SectionName = "PaymobSettings";
    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public int CardIntegrationId { get; set; } // Found in Paymob Dashboard -> Integrations
    public string BaseUrl { get; set; } = "https://accept.paymob.com/v1";
}