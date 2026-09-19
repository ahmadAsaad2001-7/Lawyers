using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Users.Queries;
public class GetNotificationsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery q, CancellationToken ct)
        => await unitOfWork.PlatformNotifications.Query().AsNoTracking()
            .Where(n => n.RecipientUserId == currentUser.UserId!.Value && (!q.UnreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt)
            .Take(q.Limit)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
}