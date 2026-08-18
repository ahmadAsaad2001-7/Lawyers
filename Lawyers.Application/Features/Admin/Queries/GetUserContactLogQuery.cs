using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetUserContactLogQuery(int UserId, int Page = 1, int PageSize = 20) 
    : IRequest<PagedResult<UserContactLogDto>>;