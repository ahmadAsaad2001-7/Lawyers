using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetPeriodAnalyticsQuery(DateTime Start, DateTime End) : IRequest<PeriodAnalyticsDto>;

public class GetPeriodAnalyticsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPeriodAnalyticsQuery, PeriodAnalyticsDto>
{
    public async Task<PeriodAnalyticsDto> Handle(GetPeriodAnalyticsQuery q, CancellationToken ct)
    {
        var consultations = await unitOfWork.Consultations.Query()
            .AsNoTracking()
            .Include(c => c.Payment)
            .Include(c => c.Lawyer)
            .Where(c => c.CreatedAt >= q.Start && c.CreatedAt <= q.End)
            .ToListAsync(ct);

        bool Paid(Domain.Entities.Consultation c) => c.Payment != null
                                                     && c.Payment.Status == Domain.Entities.Enums.PaymentStatus.Succeeded;

        var completed = consultations.Count(c =>
            c.Status == Domain.Entities.Enums.ConsultationStatus.Completed);

        var quarterRevenue = await unitOfWork.Consultations.Query().AsNoTracking()
            .Where(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-90))
            .Where(c => c.Payment != null && c.Payment.Status == Domain.Entities.Enums.PaymentStatus.Succeeded)
            .SumAsync(c => c.Payment!.Amount, ct);

        var newUsers = await unitOfWork.Users.Query().AsNoTracking()
            .CountAsync(u => u.CreatedAt >= q.Start && u.CreatedAt <= q.End, ct);

        var topLawyers = consultations
            .Where(Paid)
            .GroupBy(c => c.LawyerId)
            .Select(g => new TopLawyerDto
            {
                UserId = g.First().Lawyer.UserId,
                FullName = g.First().Lawyer.FullName,
                Revenue = g.Sum(c => c.Payment!.Amount),
                Consultations = g.Count(),
                AverageRating = g.First().Lawyer.AverageRating,
            })
            .OrderByDescending(t => t.Revenue)
            .Take(5)
            .ToList();

        return new PeriodAnalyticsDto
        {
            PeriodRevenue = consultations.Where(Paid).Sum(c => c.Payment!.Amount),
            QuarterRevenue = quarterRevenue,
            ConsultationsCount = consultations.Count,
            CompletedCount = completed,
            CompletionRate = consultations.Count == 0 ? 0 : completed * 100m / consultations.Count,
            NewUsersCount = newUsers,
            TopLawyers = topLawyers,
        };
    }
}
