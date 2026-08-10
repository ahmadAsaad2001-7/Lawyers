using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Consultations.Queries;

public record GetMyConsultationsQuery : IRequest<List<ConsultationSummaryDto>>;

public class ConsultationSummaryDto
{
    public int Id { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserImageUrl { get; set; }
    public string OtherUserRole { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public int? LastMessageSenderId { get; set; }
    public bool IsOnline { get; set; }
    public int UnreadCount { get; set; } // 👈 ADD THIS HERE
}

public class GetMyConsultationsHandler : IRequestHandler<GetMyConsultationsQuery, List<ConsultationSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyConsultationsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<ConsultationSummaryDto>> Handle(GetMyConsultationsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User must be authenticated");

        // 1. Fetch all consultations where the user is either the Client or the Lawyer// Inside GetMyConsultationsHandler.cs

// 1. Fetch all consultations where the user is either the Client or the Lawyer
        var consultations = await _unitOfWork.Consultations.Query()
            .Include(c => c.Client).ThenInclude(client => client.User) // 👈 ADD THIS HERE
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Where(c => c.Client.UserId == currentUserId || c.Lawyer.UserId == currentUserId)
            .ToListAsync(cancellationToken);

        if (!consultations.Any()) return new List<ConsultationSummaryDto>();

        // 2. Fetch the last message for each consultation in a single optimized query
        var consultationIds = consultations.Select(c => c.Id).ToList();
        var lastMessages = await _unitOfWork.Messages.Query()
            .Where(m => consultationIds.Contains(m.ConsultationId))
            .GroupBy(m => m.ConsultationId)
            .Select(g => new 
            {
                ConsultationId = g.Key,
                LastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var lastMessageDict = lastMessages.ToDictionary(x => x.ConsultationId, x => x.LastMessage);

        // 3. Map to DTO
        var result = new List<ConsultationSummaryDto>();
        foreach (var c in consultations)
        {
            var isClient = c.Client.UserId == currentUserId;
            lastMessageDict.TryGetValue(c.Id, out var lastMsg);

            result.Add(new ConsultationSummaryDto
            {
                Id = c.Id,
                // 👇 Use the Profile's FullName, not the Identity User's UserName (which is usually the email)
                OtherUserName = isClient ? c.Lawyer.FullName : c.Client.FullName, 
                OtherUserImageUrl = isClient ? c.Lawyer.User.ProfileImageUrl : c.Client.User.ProfileImageUrl,
                OtherUserRole = isClient ? "Lawyer" : "Client",
                Status = c.Status.ToString(),
                ScheduledAt = c.ScheduledAt,
                LastMessageContent = lastMsg?.Content,
                LastMessageDate = lastMsg?.CreatedAt,
                LastMessageSenderId = lastMsg?.SenderId,
                IsOnline = false // Can be hooked to SignalR presence later
            });
        }

        // Sort by most recent message, fallback to scheduled date
        return result.OrderByDescending(x => x.LastMessageDate ?? x.ScheduledAt).ToList();
    }
}