using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetDashBoardStatsQuery():IRequest<DashBoardStatsDto>;