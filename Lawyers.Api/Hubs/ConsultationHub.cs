using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Lawyers.Api.Hubs;

[Authorize]
public class ConsultationHub : Hub
{
    private sealed class ActiveCall
    {
        public int CallerUserId { get; init; }
        public int CalleeUserId { get; init; }
        public string CallerConnectionId { get; init; } = string.Empty;
        public string? AnswererConnectionId { get; set; }
        public int? AnswererUserId { get; set; }
    }

    // A call has exactly two peers. Keeping this small signaling record avoids
    // delivering SDP/ICE to another tab that happens to be in the same chat.
    private static readonly ConcurrentDictionary<int, ActiveCall> ActiveCalls = new();
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ConsultationHub(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    private int GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    private async Task EnsureParticipantAsync(int consultationId)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            throw new HubException("You must be authenticated.");

        // ✅ ADMIN BYPASS
        var isAdmin = Context.User?.IsInRole("Admin") == true;
        if (isAdmin) return;

        var isParticipant = await _unitOfWork.Consultations.Query()
            .AnyAsync(c => c.Id == consultationId &&
                           (c.Client.UserId == userId || c.Lawyer.UserId == userId));

        if (!isParticipant)
            throw new HubException("You are not a participant in this consultation.");
    }

    private async Task EnsureChatIsOpenAsync(int consultationId)
    {
        // ✅ ADMIN BYPASS
        var isAdmin = Context.User?.IsInRole("Admin") == true;
        if (isAdmin) return;

        var isOpen = await _unitOfWork.Consultations.Query()
            .AnyAsync(c => c.Id == consultationId &&
                           (c.Status == ConsultationStatus.Confirmed || c.Status == ConsultationStatus.InProgress));

        if (!isOpen)
            throw new HubException("This consultation is not open for chat or calls.");
    }

    // 🔗 Add user to personal group for targeted notifications (e.g. "New Booking")
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        Console.WriteLine($"[SignalR] User {userId} connected with ConnectionId {Context.ConnectionId}");
        if (userId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            Console.WriteLine($"[SignalR] Added user {userId} to group user_{userId}");
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

        foreach (var call in ActiveCalls.Where(x =>
                     x.Value.CallerConnectionId == Context.ConnectionId ||
                     x.Value.AnswererConnectionId == Context.ConnectionId).ToList())
        {
            if (ActiveCalls.TryRemove(call.Key, out var endedCall))
            {
                var otherConnectionId = endedCall.CallerConnectionId == Context.ConnectionId
                    ? endedCall.AnswererConnectionId
                    : endedCall.CallerConnectionId;
                if (!string.IsNullOrEmpty(otherConnectionId))
                    await Clients.Client(otherConnectionId).SendAsync("UserEndedCall", new { consultationId = call.Key });
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConsultation(int consultationId)
    {
        await EnsureParticipantAsync(consultationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, consultationId.ToString());
        var userId = GetCurrentUserId();
        
        await Clients.OthersInGroup(consultationId.ToString())
                     .SendAsync("UserJoinedRoom", new { ConnectionId = Context.ConnectionId, UserId = userId });
    }

    public async Task LeaveConsultation(int consultationId)
    {
        await EnsureParticipantAsync(consultationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, consultationId.ToString());
        var userId = GetCurrentUserId();
        
        await Clients.OthersInGroup(consultationId.ToString())
                     .SendAsync("UserLeftRoom", new { ConnectionId = Context.ConnectionId, UserId = userId });
    }

    // 💾 SAVE TO DB + BROADCAST
    public async Task SendMessage(int consultationId, string content)
    {
        var userId = GetCurrentUserId();
        await EnsureParticipantAsync(consultationId);
        await EnsureChatIsOpenAsync(consultationId);
        if (string.IsNullOrWhiteSpace(content)) return;

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

        var otherUserId = await GetOtherParticipantIdAsync(consultationId);
        if (otherUserId > 0 && otherUserId != userId)
        {
            var preview = content.Length <= 80 ? content : content[..80] + "...";
            await _notificationService.NotifyAsync(otherUserId, "رسالة جديدة", preview);
        }
    }

    // 📜 HISTORY FETCH (Called by frontend on load)
    public async Task<IEnumerable<object>> GetRecentMessages(int consultationId, int count = 50)
    {
        await EnsureParticipantAsync(consultationId);
        count = Math.Clamp(count, 1, 100);
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

    // WebRTC signaling is deliberately routed between the exact two peers,
    // matching an offer/answer call session rather than broadcasting to a room.
    public async Task SendWebRtcOffer(int consultationId, object offer)
    {
        await EnsureParticipantAsync(consultationId);
        await EnsureChatIsOpenAsync(consultationId);
        if (!ActiveCalls.TryGetValue(consultationId, out var call) ||
            call.CallerConnectionId != Context.ConnectionId ||
            string.IsNullOrEmpty(call.AnswererConnectionId))
            throw new HubException("There is no accepted call for this offer.");

        await Clients.Client(call.AnswererConnectionId).SendAsync("ReceiveWebRtcOffer", offer);
    }

    public async Task SendWebRtcAnswer(int consultationId, object answer)
    {
        await EnsureParticipantAsync(consultationId);
        await EnsureChatIsOpenAsync(consultationId);
        if (!ActiveCalls.TryGetValue(consultationId, out var call) ||
            call.AnswererConnectionId != Context.ConnectionId)
            throw new HubException("There is no matching call for this answer.");

        await Clients.Client(call.CallerConnectionId).SendAsync("ReceiveWebRtcAnswer", answer);
    }

    public async Task SendIceCandidate(int consultationId, object candidate)
    {
        await EnsureParticipantAsync(consultationId);
        await EnsureChatIsOpenAsync(consultationId);
        if (!ActiveCalls.TryGetValue(consultationId, out var call))
            throw new HubException("There is no active call for this ICE candidate.");

        var destination = call.CallerConnectionId == Context.ConnectionId
            ? call.AnswererConnectionId
            : call.AnswererConnectionId == Context.ConnectionId
                ? call.CallerConnectionId
                : null;
        if (string.IsNullOrEmpty(destination))
            throw new HubException("You are not a peer in this active call.");

        await Clients.Client(destination).SendAsync("ReceiveIceCandidate", candidate);
    }

    public async Task EndCall(int consultationId)
    {
        await EnsureParticipantAsync(consultationId);
        if (ActiveCalls.TryRemove(consultationId, out var call))
        {
            var destination = call.CallerConnectionId == Context.ConnectionId
                ? call.AnswererConnectionId
                : call.CallerConnectionId;
            if (!string.IsNullOrEmpty(destination) && destination != Context.ConnectionId)
                await Clients.Client(destination).SendAsync("UserEndedCall", new { consultationId });
        }
    }
    // ==========================================
// 5. Call Request / Accept / Reject
// ==========================================
    public async Task RequestCall(int consultationId, string mode)
    {
        await EnsureParticipantAsync(consultationId);
        await EnsureChatIsOpenAsync(consultationId);
        if (mode is not ("audio" or "video"))
            throw new HubException("Unsupported call mode.");
        var call = new ActiveCall
        {
            CallerUserId = GetCurrentUserId(),
            CallerConnectionId = Context.ConnectionId,
            CalleeUserId = await GetOtherParticipantIdAsync(consultationId)
        };
        if (!ActiveCalls.TryAdd(consultationId, call))
            throw new HubException("A call is already in progress for this consultation.");

        await Clients.Group($"user_{call.CalleeUserId}").SendAsync("IncomingCall", new { mode, consultationId });
    }

    public async Task AcceptCall(int consultationId)
    {
        await EnsureParticipantAsync(consultationId);
        if (!ActiveCalls.TryGetValue(consultationId, out var call) ||
            call.CallerConnectionId == Context.ConnectionId ||
            call.AnswererConnectionId != null ||
            call.CalleeUserId != GetCurrentUserId())
            throw new HubException("There is no call available to accept.");

        call.AnswererConnectionId = Context.ConnectionId;
        call.AnswererUserId = GetCurrentUserId();
        await Clients.Client(call.CallerConnectionId).SendAsync("CallAccepted", new { consultationId });
    }

    public async Task RejectCall(int consultationId)
    {
        await EnsureParticipantAsync(consultationId);
        if (ActiveCalls.TryRemove(consultationId, out var call))
            await Clients.Client(call.CallerConnectionId).SendAsync("CallRejected", new { consultationId });
    }

    private async Task<int> GetOtherParticipantIdAsync(int consultationId)
    {
        var currentUserId = GetCurrentUserId();
        var consultation = await _unitOfWork.Consultations.Query()
            .Where(c => c.Id == consultationId)
            .Select(c => new { ClientUserId = c.Client.UserId, LawyerUserId = c.Lawyer.UserId })
            .SingleAsync();

        return consultation.ClientUserId == currentUserId
            ? consultation.LawyerUserId
            : consultation.ClientUserId;
    }
}
