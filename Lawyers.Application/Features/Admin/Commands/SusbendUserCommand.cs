using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public record SuspendUserCommand(int UserId, string Reason, int Days) : IRequest<bool>;


public class SuspendUserCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, ICurrentUserService currentUser)
    : IRequestHandler<SuspendUserCommand, bool>
{
    public async Task<bool> Handle(SuspendUserCommand cmd, CancellationToken ct)
    {
        var existing = await unitOfWork.UserSuspensions.Query()
            .FirstOrDefaultAsync(s => s.UserId == cmd.UserId && s.IsActive, ct);
        if (existing != null)
            throw new InvalidOperationException("User is already suspended.");

        var user = await userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user == null) return false;

        var suspension = new UserSuspension
        {
            UserId = cmd.UserId,
            Reason = cmd.Reason,
            StartedAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(cmd.Days),
            IsActive = true
        };

        await unitOfWork.UserSuspensions.AddAsync(suspension, ct);
        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, suspension.EndsAt);
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}