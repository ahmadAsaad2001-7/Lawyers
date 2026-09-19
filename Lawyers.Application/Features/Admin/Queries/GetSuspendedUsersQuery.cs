using Lawyers.Application.DTOs;
using Lawyers.Application.Features.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetSuspendedUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<SuspendedUserDto>>;
