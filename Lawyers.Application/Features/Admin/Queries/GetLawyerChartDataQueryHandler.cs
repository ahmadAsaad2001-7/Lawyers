using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetLawyerChartDataQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLawyerChartDataQuery, List<ChartDataPointDto>>
{
    public async Task<List<ChartDataPointDto>> Handle(GetLawyerChartDataQuery request, CancellationToken ct)
    {
        // Fetch only this lawyer's consultations
        var consultations = await unitOfWork.Consultations.Query()
            .AsNoTracking()
            .Where(c => c.LawyerId == request.LawyerProfileId)
            .Where(c => c.CreatedAt >= request.Start && c.CreatedAt <= request.End)
            .Select(c => new
            {
                c.CreatedAt,
                Revenue = c.Payment != null && c.Payment.Status == Domain.Entities.Enums.PaymentStatus.Succeeded
                    ? c.Payment.Amount
                    : 0m
            })
            .ToListAsync(ct);

        var period = request.Period.Trim().ToLowerInvariant();
        var chartData = consultations
            .GroupBy(c => GetPeriodStart(c.CreatedAt, period))
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPointDto
            {
                Date = g.Key,
                Label = period == "monthly" ? g.Key.ToString("MMM yyyy") : g.Key.ToString("dd MMM"),
                Consultations = g.Count(),
                Revenue = g.Sum(c => c.Revenue)
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
