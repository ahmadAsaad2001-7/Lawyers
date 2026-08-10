

using MediatR;

namespace Lawyers.Application.Features.Payments.Commands;

public class NotifyPaymentSuccessCommand : IRequest<bool>
{
    public int ConsultationId { get; set; }
    
    public string GatewayPaymentId { get; set; } = string.Empty; // Used to UPDATE the record
    public string Status { get; set; } = string.Empty;
}
