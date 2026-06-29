namespace Lawyers.Application.DTOs;

public class AuthorizePaymentResponseDto
{
    public string TransactionId { get; set; } = string.Empty; // The ID from Kashier
    public string CheckoutUrl { get; set; } = string.Empty;   // The URL to redirect the user to
}