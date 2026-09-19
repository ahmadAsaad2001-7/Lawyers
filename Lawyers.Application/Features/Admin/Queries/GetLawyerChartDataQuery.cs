
using MediatR;
using Lawyers.Application.DTOs;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetLawyerChartDataQuery(int LawyerProfileId, DateTime Start, DateTime End, string Period = "daily")
    : IRequest<List<ChartDataPointDto>>;