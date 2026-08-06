using MediatR;

namespace Lawyers.Application.Features.Consultation.Events;

public class ConsultationConfirmedEvent : INotification
{
    public int ConsultationId { get; set; }
    public int ClientId { get; set; }
    public int LawyerId { get; set; }
    public string ClientEmail { get; set; } = string.Empty;
    public string LawyerEmail { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
}