using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Users.Commands;

public class MarkNotificationReadCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<MarkNotificationReadCommand, bool>
{
    public async Task<bool> Handle(MarkNotificationReadCommand q, CancellationToken ct)
    {
        var n = await unitOfWork.PlatformNotifications.Query()
            .FirstOrDefaultAsync(x => x.Id == q.Id && x.RecipientUserId == currentUser.UserId!.Value, ct);

        if (n == null) return false;

        n.IsRead = true;
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}