using Lawyers.Application.Enums;

namespace Lawyers.Application.DTOs;

public record InitiatePaymentDto(
    decimal Amount,
    string Currency,
    int ConsultationId,
    string CustomerEmail,
    PaymentChannel Channel,
    string? CardToken = null,       // Required if Channel == Card (Saved token)
    string? WalletPhoneNumber = null // Required if Channel == MobileWallet (e.g., 01012345678)
);