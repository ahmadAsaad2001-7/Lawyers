using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Lawyers.Application.Features.Consultations.Queries;

// ✅ Response DTO with all data needed for chat UI
public class ConsultationDetailsResponse
{
    public int ConsultationId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserImageUrl { get; set; }
    public string OtherUserRole { get; set; } = string.Empty; // "Lawyer" or "Client"
    public ConsultationStatus Status { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    
    // Last Message Info
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public int? LastMessageSenderId { get; set; }
    
    // Online Status (you can enhance this with SignalR presence later)
    public bool IsOnline { get; set; } = false;
}

// ✅ Query
public record ConsultationDetailsQuery(int ConsultationId) : IRequest<ConsultationDetailsResponse>;

// ✅ Handler Implementation
public class ConsultationDetailsHandler : IRequestHandler<ConsultationDetailsQuery, ConsultationDetailsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ConsultationDetailsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ConsultationDetailsResponse> Handle(ConsultationDetailsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User must be authenticated");

        // Fetch consultation with related data
        var consultation = await _unitOfWork.Consultations.Query()
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Payment)
            .FirstOrDefaultAsync(c => 
                c.Id == request.ConsultationId && 
                (c.Client.UserId == currentUserId || c.Lawyer.UserId == currentUserId),
                cancellationToken);

        if (consultation == null)
            throw new Exception("Consultation not found or access denied.");

        // Determine if current user is client or lawyer
       
        var isClient = consultation.Client.UserId == currentUserId;

        // ✅ FIX: Use the Profile's FullName
        var otherUserName = isClient 
            ? consultation.Lawyer.FullName 
            : consultation.Client.FullName;

        var otherUserImageUrl = isClient
            ? consultation.Lawyer.User.ProfileImageUrl   
            : consultation.Client.User.ProfileImageUrl;
        // Get the "other" user (who you're chatting with)
        var otherUser = isClient ? consultation.Lawyer.User : consultation.Client.User;

        // Fetch last message for this consultation
        var lastMessage = await _unitOfWork.Messages.Query()
            .Where(m => m.ConsultationId == consultation.Id)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new ConsultationDetailsResponse
        {
            ConsultationId = consultation.Id,
            OtherUserName = otherUserName,
            OtherUserImageUrl = otherUserImageUrl,
            OtherUserRole = isClient ? "Lawyer" : "Client",
            Status = consultation.Status,
            ScheduledAt = consultation.ScheduledAt,
            DurationMinutes = consultation.DurationMinutes,
            LastMessageContent = lastMessage?.Content,
            LastMessageDate = lastMessage?.CreatedAt,
            LastMessageSenderId = lastMessage?.SenderId,
            IsOnline = false
        };
    }
}