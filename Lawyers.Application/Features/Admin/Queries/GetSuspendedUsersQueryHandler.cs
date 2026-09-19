using Lawyers.Application.DTOs;
using Lawyers.Application.Features.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetSuspendedUsersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSuspendedUsersQuery, PagedResult<SuspendedUserDto>>
{
    public async Task<PagedResult<SuspendedUserDto>> Handle(GetSuspendedUsersQuery request, CancellationToken ct)
    {
        var query = unitOfWork.UserSuspensions.Query()
            .AsNoTracking()
            .Include(s => s.User)
            .ThenInclude(u => u.ClientProfile)
            .Include(s => s.User)
            .ThenInclude(u => u.LawyerProfile)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.StartedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SuspendedUserDto
            {
                UserId = s.UserId,
                DisplayName = s.User.LawyerProfile != null ? s.User.LawyerProfile.FullName
                    : s.User.ClientProfile != null ? s.User.ClientProfile.FullName
                    : s.User.UserName!,
                Email = s.User.Email!,
                ProfileImageUrl = s.User.ProfileImageUrl,
                Reason = s.Reason,
                StartedAt = s.StartedAt,
                EndsAt = s.EndsAt,
                RemainingDays = (int)Math.Max(0, (s.EndsAt - DateTime.UtcNow).TotalDays)
            })
            .ToListAsync(ct);

        return new PagedResult<SuspendedUserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }
}