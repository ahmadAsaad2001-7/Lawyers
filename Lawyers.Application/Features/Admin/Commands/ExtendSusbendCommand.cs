using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public record ExtendSuspendCommand(int UserId, int AdditionalDays) : IRequest<bool>;

public class ExtendSuspendUserCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager)
    : IRequestHandler<ExtendSuspendCommand, bool>
{
    public async Task<bool> Handle(ExtendSuspendCommand cmd, CancellationToken ct)
    {
        // 1. Find the ACTIVE suspension (user MUST be suspended to extend)
        var existing = await unitOfWork.UserSuspensions.Query()
            .FirstOrDefaultAsync(s => s.UserId == cmd.UserId && s.IsActive, ct);

        if (existing == null)
            throw new InvalidOperationException("User is not currently suspended. Cannot extend.");

        var user = await userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user == null) return false;

        // 2. Push EndsAt forward by the additional days
        existing.EndsAt = existing.EndsAt.AddDays(cmd.AdditionalDays);

        // 3. Update the Identity lockout to the new end date
        await userManager.SetLockoutEndDateAsync(user, existing.EndsAt);
        await userManager.UpdateAsync(user);

        // 4. Save the updated suspension record
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}