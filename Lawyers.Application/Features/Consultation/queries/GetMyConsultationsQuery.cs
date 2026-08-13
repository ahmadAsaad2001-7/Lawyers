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
    public int UnreadCount { get; set; }
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

        // A conversation belongs only to its client and lawyer.  Admins can see
        // consultations they created themselves, but must never inherit every
        // other user's private conversations.
        IQueryable<Domain.Entities.Consultation> query = _unitOfWork.Consultations.Query()
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Where(c => c.Client.UserId == currentUserId || c.Lawyer.UserId == currentUserId);

        var consultations = await query.ToListAsync(cancellationToken);

        if (!consultations.Any()) return new List<ConsultationSummaryDto>();

        var consultationIds = consultations.Select(c => c.Id).ToList();

        // Last message per consultation (single optimized query)
        var lastMessages = await _unitOfWork.Messages.Query()
            .Where(m => consultationIds.Contains(m.ConsultationId))
            .GroupBy(m => m.ConsultationId)
            .Select(g => new
            {
                ConsultationId = g.Key,
                LastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var lastMessageDict = lastMessages.ToDictionary(x => x.ConsultationId, x => x.LastMessage!);

        // Unread count per consultation = messages sent by the OTHER user
        var unreadCounts = await _unitOfWork.Messages.Query()
            .Where(m => consultationIds.Contains(m.ConsultationId) && m.SenderId != currentUserId)
            .GroupBy(m => m.ConsultationId)
            .Select(g => new { ConsultationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConsultationId, x => x.Count, cancellationToken);

        // Map to DTO
        var result = new List<ConsultationSummaryDto>();
        foreach (var c in consultations)
        {
            var isClient = c.Client.UserId == currentUserId;
            lastMessageDict.TryGetValue(c.Id, out var lastMsg);
            unreadCounts.TryGetValue(c.Id, out var unread);

            result.Add(new ConsultationSummaryDto
            {
                Id = c.Id,
                OtherUserName = isClient ? c.Lawyer.FullName : c.Client.FullName,
                OtherUserImageUrl = isClient ? c.Lawyer.User.ProfileImageUrl : c.Client.User.ProfileImageUrl,
                OtherUserRole = isClient ? "Lawyer" : "Client",
                Status = c.Status.ToString(),
                ScheduledAt = c.ScheduledAt,
                LastMessageContent = lastMsg?.Content,
                LastMessageDate = lastMsg?.CreatedAt,
                LastMessageSenderId = lastMsg?.SenderId,
                UnreadCount = unread,
                IsOnline = false
            });
        }

        return result.OrderByDescending(x => x.LastMessageDate ?? x.ScheduledAt).ToList();
    }
}
