using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Users.Queries;

public class GetUnreadNotificationCountQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public Task<int> Handle(GetUnreadNotificationCountQuery q, CancellationToken ct)
        => unitOfWork.PlatformNotifications.Query()
            .CountAsync(n => n.RecipientUserId == currentUser.UserId!.Value && !n.IsRead, ct);
}