using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Lawyers.Application.Features.Admin.Queries;

public class GetAllVotesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetAllVotesQuery, PagedResult<AdminVoteDto>>
{
    public async Task<PagedResult<AdminVoteDto>> Handle(GetAllVotesQuery q, CancellationToken ct)
    {
        var adminId = currentUser.UserId!.Value;
        var query = unitOfWork.AdminVotes.Query().AsNoTracking()
            .Where(v => q.IncludeResolved || !v.IsResolved)
            .OrderByDescending(v => v.CreatedAt);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(v => new AdminVoteDto
            {
                Id = v.Id,
                ActionType = v.ActionType,
                TargetUserId = v.TargetUserId,
                TargetUserName = v.TargetUser.LawyerProfile != null ? v.TargetUser.LawyerProfile.FullName
                    : v.TargetUser.ClientProfile != null ? v.TargetUser.ClientProfile.FullName
                    : v.TargetUser.UserName!,
                TargetEmail = v.TargetUser.Email!,                         
                TargetProfileImageUrl = v.TargetUser.ProfileImageUrl,       // ✅ added — same reason
                InitiatorName = v.InitiatorAdmin != null                   // ✅ FIXED — null-safe
                    ? v.InitiatorAdmin.UserName!
                    : "Self-applied",
                IsSelfApplied = v.InitiatorAdminId == null,                 // ✅ added — same as GetVotesQueryHandler
                Reason = v.Reason,
                ApprovalCount = v.ApprovalCount,
                DisapprovalCount = v.DisapprovalCount,
                IsResolved = v.IsResolved,
                CreatedAt = v.CreatedAt,
                HasCurrentAdminVoted = v.Participants.Any(p => p.AdminUserId == adminId)
            }).ToListAsync(ct);
        return new PagedResult<AdminVoteDto> { Items = items, TotalCount = totalCount, PageNumber = q.Page, PageSize = q.PageSize };
    }
}