

using MediatR;

namespace Lawyers.Application.Features.Payments.Commands;

public class NotifyPaymentSuccessCommand : IRequest<bool>
{
    public int ConsultationId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
