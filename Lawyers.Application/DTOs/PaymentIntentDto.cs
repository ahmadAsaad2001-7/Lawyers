namespace Lawyers.Application.DTOs;

public record PaymentIntentResponseDto(
    string PaymentIntentId,
    string ClientSecret,   
    decimal Amount,
    string Currency
);