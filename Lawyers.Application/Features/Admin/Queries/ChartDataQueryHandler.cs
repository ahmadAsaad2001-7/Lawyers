using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetChartDataQueryHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetChartDataQuery, List<ChartDataPointDto>>
{
    public async Task<List<ChartDataPointDto>> Handle(GetChartDataQuery request, CancellationToken ct)
    {
        var consultations = await unitOfWork.Consultations.Query()
            .AsNoTracking()
            .Where(consultation => consultation.CreatedAt >= request.Start && consultation.CreatedAt <= request.End)
            .Select(consultation => new
            {
                consultation.CreatedAt,
                Revenue = consultation.Payment != null && consultation.Payment.Status == Domain.Entities.Enums.PaymentStatus.Succeeded
                    ? consultation.Payment.Amount
                    : 0m
            })
            .ToListAsync(ct);

        var period = request.Period.Trim().ToLowerInvariant();
        var chartData = consultations
            .GroupBy(consultation => GetPeriodStart(consultation.CreatedAt, period))
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPointDto
            {
                Date = g.Key,
                Label = period == "monthly" ? g.Key.ToString("MMM yyyy") : g.Key.ToString("dd MMM"),
                Consultations = g.Count(),
                Revenue = g.Sum(consultation => consultation.Revenue)
            })
            .ToList();

        return chartData;
    }

    private static DateTime GetPeriodStart(DateTime date, string period) => period switch
    {
        "weekly" => date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7),
        "monthly" => new DateTime(date.Year, date.Month, 1),
        _ => date.Date
    };
}
