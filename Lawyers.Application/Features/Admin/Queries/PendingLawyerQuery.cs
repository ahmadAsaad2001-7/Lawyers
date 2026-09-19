using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetPendingLawyersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<PendingLawyerDto>>;