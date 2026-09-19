using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;


public record GetUserChartDataQuery(int UserId, DateTime Start, DateTime End, string Period = "daily") 
    : IRequest<List<ChartDataPointDto>>;