namespace Lawyers.Application.Interfaces;

public interface INotificationService
{
    Task SendBookingConfirmedAsync(int clientId, int lawyerId, DateTime scheduledAt);
    Task SendNewBookingAsync(int clientId, int lawyerId, DateTime scheduledAt);
    
    // Add other notification types as needed
}