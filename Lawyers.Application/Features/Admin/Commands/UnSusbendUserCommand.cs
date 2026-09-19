using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public record UnsuspendUserCommand(int UserId) : IRequest<bool>;

public class UnSuspendUserCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager)
    : IRequestHandler<UnsuspendUserCommand, bool>
{
    public async Task<bool> Handle(UnsuspendUserCommand cmd, CancellationToken ct)
    {
        // 1. Find the active suspension
        var existing = await unitOfWork.UserSuspensions.Query()
            .FirstOrDefaultAsync(s => s.UserId == cmd.UserId && s.IsActive, ct);
            
        if (existing == null)
        {
            // User isn't suspended, nothing to do
            throw new InvalidOperationException("User is not currently suspended.");
        }

        var user = await userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user == null) return false;

        // 2. Terminate the suspension record early
        existing.IsActive = false;
        existing.EndsAt = DateTime.UtcNow; // Records exactly when the admin unsuspended them

        // 3. Clear the Identity lockout so they can log in immediately
        // Passing 'null' removes the lockout end date entirely
        await userManager.SetLockoutEndDateAsync(user, (DateTimeOffset?)null); 
        
        // Optional: If you want to turn off the lockout feature completely for this user
        // await userManager.SetLockoutEnabledAsync(user, false);

        await unitOfWork.SaveChangesAsync(ct);
        await userManager.UpdateAsync(user);

        return true;
    }
}