using Lawyers.Application.Features.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;


public class GetSuspendedUserByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSuspendedUserByIdQuery, SuspendedUserDto?>
{
    public async Task<SuspendedUserDto?> Handle(GetSuspendedUserByIdQuery request, CancellationToken ct)
    {
        var suspension = await unitOfWork.UserSuspensions.Query()
            .AsNoTracking()
            .Include(s => s.User)
            .ThenInclude(u => u.ClientProfile)
            .Include(s => s.User)
            .ThenInclude(u => u.LawyerProfile)
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.IsActive, ct);

        if (suspension == null)
            return null;

        var remainingDays = Math.Max(0, (suspension.EndsAt - DateTime.UtcNow).Days);

        return new SuspendedUserDto
        {
            UserId = suspension.UserId,
            DisplayName = suspension.User.LawyerProfile?.FullName 
                          ?? suspension.User.ClientProfile?.FullName 
                          ?? suspension.User.UserName ?? suspension.User.Email!,
            Email = suspension.User.Email!,
            ProfileImageUrl = suspension.User.ProfileImageUrl,
            Reason = suspension.Reason,
            StartedAt = suspension.StartedAt,
            EndsAt = suspension.EndsAt,
            RemainingDays = remainingDays
        };
    }
}