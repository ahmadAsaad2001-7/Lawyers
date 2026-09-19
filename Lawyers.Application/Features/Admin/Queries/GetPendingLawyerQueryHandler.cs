using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetPendingLawyersQueryHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetPendingLawyersQuery, PagedResult<PendingLawyerDto>>
{
    public async Task<PagedResult<PendingLawyerDto>> Handle(GetPendingLawyersQuery request, CancellationToken ct)
    {
        var query = unitOfWork.LawyerProfiles.Query()
            .Include(lp => lp.User)
            .Where(lp => !lp.IsVerified && !lp.IsDeleted && lp.User.EmailConfirmed)
            .OrderBy(lp => lp.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(lp => new PendingLawyerDto
            {
                UserId = lp.UserId,
                FullName = lp.FullName,
                Email = lp.User.Email!,
                BarLicenseNumber = lp.BarLicenseNumber,
                Specialization = lp.Specialization,
                LawFirmName = lp.LawFirmName,
                RegisteredAt = lp.User.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<PendingLawyerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }
}