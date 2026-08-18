using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetAllUsersQuery(
    string? Search,            
    string? RoleFilter,       
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<AdminUserListDto>>;