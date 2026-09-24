using Lawyers.Application.Interfaces;
using Lawyers.Api.Hubs;
using Lawyers.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Lawyers.Api.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<ConsultationHub> _hubContext;
    private readonly IUnitOfWork _unitOfWork;

    public SignalRNotificationService(IHubContext<ConsultationHub> hubContext, IUnitOfWork unitOfWork)
    {
        _hubContext = hubContext;
        _unitOfWork = unitOfWork;
    }

    public async Task SendBookingConfirmedAsync(int clientId, int lawyerId, DateTime scheduledAt, int consultationId)
    {
        var when = scheduledAt.ToString("dd/MM/yyyy hh:mm tt");
        await NotifyAsync(clientId, "تم تأكيد الحجز", $"تم تأكيد استشارتك في {when}", consultationId: consultationId);
        await NotifyAsync(lawyerId, "حجز جديد", $"لديك استشارة جديدة في {when}", consultationId: consultationId);
    }

    public Task SendNewBookingAsync(int clientId, int lawyerId, DateTime scheduledAt, int consultationId)
    {
        _ = clientId;
        var when = scheduledAt.ToString("dd/MM/yyyy hh:mm tt");
        return NotifyAsync(lawyerId, "طلب حجز جديد",
            $"لديك طلب حجز جديد في {when} بانتظار تأكيد الدفع", consultationId: consultationId);
    }

    public Task SendGenericNotificationAsync(int recipientUserId, string title, string message)
        => NotifyAsync(recipientUserId, title, message);

    public async Task NotifyAsync(int recipientUserId, string title, string message, CancellationToken ct = default, int? consultationId = null)
    {
        var notification = new PlatformNotification
        {
            RecipientUserId = recipientUserId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PlatformNotifications.AddAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            await _hubContext.Clients.Group($"user_{recipientUserId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    notification.IsRead,
                    notification.CreatedAt,
                    ConsultationId = consultationId
                }, ct);
        }
        catch
        {
            // Live push is best-effort; the persisted row is the source of truth.
        }
    }
}
