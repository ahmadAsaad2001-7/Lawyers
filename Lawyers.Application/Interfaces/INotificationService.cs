namespace Lawyers.Application.Interfaces;

public interface INotificationService
{
    Task SendBookingConfirmedAsync(int clientId, int lawyerId, DateTime scheduledAt, int consultationId);
    Task SendNewBookingAsync(int clientId, int lawyerId, DateTime scheduledAt, int consultationId);
    
    // Add other notification types as needed
    Task SendGenericNotificationAsync(int recipientUserId, string title, string message);
    Task NotifyAsync(int recipientUserId, string title, string message, CancellationToken ct = default, int? consultationId = null);
}