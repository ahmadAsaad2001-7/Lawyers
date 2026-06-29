using Lawyers.Application.DTOs;

namespace Lawyers.Application.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Step 1 (Booking): Creates a payment intent that AUTHORIZES the card but DOES NOT CHARGE IT.
    /// This supports the "Escrow" requirement.
    /// </summary>
    Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(decimal amount, string currency, int consultationId, string customerEmail);

    /// <summary>
    /// Step 2 (Start Call): Captures the previously authorized funds. 
    /// To be called when the consultation officially begins.
    /// </summary>
    Task<bool> CapturePaymentAsync(string paymentIntentId);

    /// <summary>
    /// Step 3 (Cancellation): Releases the hold on the client's card.
    /// To be called if the lawyer rejects the booking or the client cancels in time.
    /// </summary>
    Task<bool> CancelPaymentAsync(string paymentIntentId);
    Task<PaymentGatewayStatus> GetPaymentStatusAsync(string transactionId);

    // Note: Disbursing to the lawyer (Stripe Connect payouts) can be handled later
    // in the MVP or done manually via the Stripe Dashboard for the first few weeks.
    
}
public enum PaymentGatewayStatus
{
    Pending,
    Authorized, // Money is held, but not captured
    Captured,   // Money is successfully taken (Success)
    Failed,
    Voided
}