using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, PagedResult<UserListDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<UserListDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.Users.Query()
            .Include(u => u.LawyerProfile)
            .Include(u => u.ClientProfile)
            .AsNoTracking()
            .AsQueryable();

        // 1. Soft-delete filter
        if (!request.IncludeDeleted)
            query = query.Where(u => !u.IsDeleted);

        // 2. Role filter (matches the Roles enum name)
        if (!string.IsNullOrWhiteSpace(request.RoleFilter))
        {
            if (!Enum.TryParse<Roles>(request.RoleFilter.Trim(), ignoreCase: true, out var role))
            {
                return new PagedResult<UserListDto>
                {
                    PageNumber = request.Page,
                    PageSize = request.PageSize
                };
            }

            query = query.Where(user => user.Role == role);
        }

        // 3. Search across email, phone, and profile full names
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(term)) ||
                (u.LawyerProfile != null && u.LawyerProfile.FullName.ToLower().Contains(term)) ||
                (u.ClientProfile != null && u.ClientProfile.FullName.ToLower().Contains(term))
            );
        }

        // 4. Pagination
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                ProfileImageUrl = u.ProfileImageUrl,
                CreatedAt = u.CreatedAt,
                IsDeleted = u.IsDeleted,

                DisplayName =
                    u.LawyerProfile != null ? u.LawyerProfile.FullName :
                    u.ClientProfile != null ? u.ClientProfile.FullName :
                    u.Email!,

                // Lawyer fields
                LawyerProfileId = u.LawyerProfile != null ? u.LawyerProfile.Id : null,
                Specialization = u.LawyerProfile != null ? u.LawyerProfile.Specialization : null,
                HourlyRate = u.LawyerProfile != null ? u.LawyerProfile.HourlyRate : null,
                IsVerified = u.LawyerProfile != null ? u.LawyerProfile.IsVerified : null,
                AverageRating = u.LawyerProfile != null ? u.LawyerProfile.AverageRating : null,
                LawFirmName = u.LawyerProfile != null ? u.LawyerProfile.LawFirmName : null,

                // Client fields
                ClientProfileId = u.ClientProfile != null ? u.ClientProfile.Id : null,
                ClientFullName = u.ClientProfile != null ? u.ClientProfile.FullName : null,

                // Counters — computed via navigation
                ConsultationCount =
                    (u.ClientProfile != null ? u.ClientProfile.Consultations.Count : 0) +
                    (u.LawyerProfile != null ? u.LawyerProfile.Consultations.Count : 0),
                PostsCount = u.LawyerProfile != null ? u.LawyerProfile.Posts.Count : 0,
                FreeMessagesCount = u.LawyerProfile != null ? u.LawyerProfile.FreeMessages.Count : 0,
            })
            .ToListAsync(ct);

        return new PagedResult<UserListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.Page,
            PageSize = request.PageSize,
        };
    }
}
