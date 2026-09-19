using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;

// Query
public record GetRecentActivityQuery(int Limit = 5) : IRequest<List<RecentActivityDto>>;

