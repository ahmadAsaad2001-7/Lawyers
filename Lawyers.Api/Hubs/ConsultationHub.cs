using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lawyers.Api.Hubs;

[Authorize]
public class ConsultationHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;

    public ConsultationHub(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private int GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    // 🔗 Add user to personal group for targeted notifications (e.g. "New Booking")
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConsultation(int consultationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, consultationId.ToString());
        var userId = GetCurrentUserId();
        
        await Clients.OthersInGroup(consultationId.ToString())
                     .SendAsync("UserJoinedRoom", new { ConnectionId = Context.ConnectionId, UserId = userId });
    }

    public async Task LeaveConsultation(int consultationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, consultationId.ToString());
        var userId = GetCurrentUserId();
        
        await Clients.OthersInGroup(consultationId.ToString())
                     .SendAsync("UserLeftRoom", new { ConnectionId = Context.ConnectionId, UserId = userId });
    }

    // 💾 SAVE TO DB + BROADCAST
    public async Task SendMessage(int consultationId, string content)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        // 1. Persist to Database
        var message = new Message
        {
            ConsultationId = consultationId,
            SenderId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Messages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        // 2. Broadcast to everyone in the room
        await Clients.Group(consultationId.ToString())
                     .SendAsync("ReceiveMessage", new 
                     {
                         Id = message.Id,
                         ConsultationId = consultationId,
                         SenderId = userId,
                         Content = content,
                         CreatedAt = message.CreatedAt
                     });
    }

    // 📜 HISTORY FETCH (Called by frontend on load)
    public async Task<IEnumerable<object>> GetRecentMessages(int consultationId, int count = 50)
    {
        var messages = await _unitOfWork.Messages.Query()
            .Where(m => m.ConsultationId == consultationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt) // Re-order ascending for UI rendering
            .Select(m => new 
            {
                m.Id,
                m.ConsultationId,
                m.SenderId,
                m.Content,
                m.CreatedAt
            })
            .ToListAsync();

        return messages;
    }

    // WebRTC Signaling (Keep your existing methods)
    public async Task SendWebRtcOffer(int consultationId, object offer) => 
        await Clients.OthersInGroup(consultationId.ToString()).SendAsync("ReceiveWebRtcOffer", offer);

    public async Task SendWebRtcAnswer(int consultationId, object answer) => 
        await Clients.OthersInGroup(consultationId.ToString()).SendAsync("ReceiveWebRtcAnswer", answer);

    public async Task SendIceCandidate(int consultationId, object candidate) => 
        await Clients.OthersInGroup(consultationId.ToString()).SendAsync("ReceiveIceCandidate", candidate);

    public async Task EndCall(int consultationId) => 
        await Clients.OthersInGroup(consultationId.ToString()).SendAsync("UserEndedCall");
}