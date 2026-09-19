using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetUserChartDataQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserChartDataQuery, List<ChartDataPointDto>>
{
    public async Task<List<ChartDataPointDto>> Handle(GetUserChartDataQuery request, CancellationToken ct)
    {
        // 1. Resolve which side of a consultation this user sits on
        var user = await unitOfWork.Users.Query()
            .AsNoTracking()
            .Include(u => u.ClientProfile)
            .Include(u => u.LawyerProfile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null) return new List<ChartDataPointDto>();

        var clientProfileId = user.ClientProfile?.Id;
        var lawyerProfileId = user.LawyerProfile?.Id;

        // 2. Fetch only this user's consultations in the date range
        var consultations = await unitOfWork.Consultations.Query()
            .AsNoTracking()
            .Where(c => c.CreatedAt >= request.Start && c.CreatedAt <= request.End)
            .Where(c =>
                (clientProfileId != null && c.ClientId == clientProfileId) ||
                (lawyerProfileId != null && c.LawyerId == lawyerProfileId))
            .Select(c => new
            {
                c.CreatedAt,
                Revenue = c.Payment != null && c.Payment.Status == Domain.Entities.Enums.PaymentStatus.Succeeded
                    ? c.Payment.Amount
                    : 0m
            })
            .ToListAsync(ct);

        // 3. Group by period (same logic as the platform chart)
        var period = request.Period.Trim().ToLowerInvariant();
        var chartData = consultations
            .GroupBy(c => GetPeriodStart(c.CreatedAt, period))
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPointDto
            {
                Date = g.Key,
                Label = period == "monthly" ? g.Key.ToString("MMM yyyy") : g.Key.ToString("dd MMM"),
                Consultations = g.Count(),
                // For a lawyer this is earnings; for a client this is spending
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
