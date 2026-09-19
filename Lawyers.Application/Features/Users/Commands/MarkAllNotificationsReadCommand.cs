using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Users.Commands;

public record MarkAllNotificationsReadCommand : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var unread = await unitOfWork.PlatformNotifications.Query()
            .Where(n => n.RecipientUserId == currentUser.UserId!.Value && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
            n.IsRead = true;

        if (unread.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return unread.Count;
    }
}
