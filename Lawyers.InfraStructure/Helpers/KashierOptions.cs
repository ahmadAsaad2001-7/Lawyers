namespace Lawyers.InfraStructure.Helpers;
public class KashierOptions
{
    public const string SectionName = "KashierSettings";
    public string MerchantId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.kashier.io/v1";
}