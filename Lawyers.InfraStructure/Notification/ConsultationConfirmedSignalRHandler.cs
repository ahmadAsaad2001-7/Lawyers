using Lawyers.Application.Features.Consultation.Events;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lawyers.Infrastructure.Notifications.SignalR;

public class ConsultationConfirmedSignalRHandler : INotificationHandler<ConsultationConfirmedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ConsultationConfirmedSignalRHandler> _logger;

    public ConsultationConfirmedSignalRHandler(
        INotificationService notificationService,
        ILogger<ConsultationConfirmedSignalRHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(ConsultationConfirmedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.SendBookingConfirmedAsync(
                notification.ClientId,
                notification.LawyerId,
                notification.ScheduledAt,
                notification.ConsultationId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking confirmation notification");
        }
    }
}