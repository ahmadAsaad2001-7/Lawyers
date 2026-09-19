using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public record DecreaseSuspendCommand(int UserId, int DaysToRemove) : IRequest<bool>;

public class DecreaseSuspendCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager)
    : IRequestHandler<DecreaseSuspendCommand, bool>
{
    public async Task<bool> Handle(DecreaseSuspendCommand cmd, CancellationToken ct)
    {
        // 1. Find the active suspension
        var existing = await unitOfWork.UserSuspensions.Query()
            .FirstOrDefaultAsync(s => s.UserId == cmd.UserId && s.IsActive, ct);

        if (existing == null)
            throw new InvalidOperationException("User is not currently suspended. Cannot decrease.");

        var user = await userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user == null) return false;

        // 2. Pull EndsAt backward by the removed days
        var newEndsAt = existing.EndsAt.AddDays(-cmd.DaysToRemove);

        // 3. EDGE CASE: if the new end date is now or earlier, auto-unsuspend
        if (newEndsAt <= DateTime.UtcNow)
        {
            // Terminate the suspension immediately
            existing.IsActive = false;
            existing.EndsAt = DateTime.UtcNow; // Record when it actually ended

            // Clear the Identity lockout entirely
            await userManager.SetLockoutEndDateAsync(user, (DateTimeOffset?)null);
        }
        else
        {
            // Normal case: just shorten the suspension
            existing.EndsAt = newEndsAt;
            await userManager.SetLockoutEndDateAsync(user, newEndsAt);
        }

        await userManager.UpdateAsync(user);
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}