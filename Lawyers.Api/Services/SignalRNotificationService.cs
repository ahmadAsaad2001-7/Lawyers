using Lawyers.Application.Interfaces;
using Lawyers.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Lawyers.Api.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<ConsultationHub> _hubContext;

    public SignalRNotificationService(IHubContext<ConsultationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendBookingConfirmedAsync(int clientId, int lawyerId, DateTime scheduledAt)
    {
        await _hubContext.Clients.Group($"user_{clientId}")
            .SendAsync("ReceiveNotification", new 
            { 
                Title = "تم تأكيد الحجز", 
                Message = $"تم تأكيد استشارتك في {scheduledAt.ToString("dd/MM/yyyy hh:mm tt")}", 
                Type = "BookingConfirmed" 
            });

        await _hubContext.Clients.Group($"user_{lawyerId}")
            .SendAsync("ReceiveNotification", new 
            { 
                Title = "حجز جديد", 
                Message = $"لديك استشارة جديدة في {scheduledAt.ToString("dd/MM/yyyy hh:mm tt")}", 
                Type = "NewBooking" 
            });
    }

    public async Task SendNewBookingAsync(int clientId, int lawyerId, DateTime scheduledAt)
    {
        // Similar implementation for new booking notifications
    }
}