using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Queries;

public record GetAvailableDaysQuery(int LawyerProfileId, int Year, int Month) : IRequest<List<int>>;

public class GetAvailableDaysQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAvailableDaysQuery, List<int>>
{
    public async Task<List<int>> Handle(GetAvailableDaysQuery q, CancellationToken ct)
    {
        var start = new DateTime(q.Year, q.Month, 1);
        var end = start.AddMonths(1);

        var slots = await unitOfWork.LawyerAvailabilities.Query().AsNoTracking()
            .Where(a => a.LawyerProfileId == q.LawyerProfileId && a.Date >= start && a.Date < end)
            .Select(a => new { a.Date, a.Hour })
            .ToListAsync(ct);

        if (slots.Count == 0) return new List<int>();

        var consultations = await unitOfWork.Consultations.Query().AsNoTracking()
            .Where(c => c.LawyerId == q.LawyerProfileId
                        && c.Status != Domain.Entities.Enums.ConsultationStatus.Cancelled
                        && c.ScheduledAt >= start && c.ScheduledAt < end)
            .Select(c => new { c.ScheduledAt, c.DurationMinutes })
            .ToListAsync(ct);

        bool IsFreeAndFuture(DateTime date, int hour)
        {
            var slotStart = date.Date.AddHours(hour);
            if (slotStart <= DateTime.UtcNow) return false;
            var slotEnd = slotStart.AddHours(1);
            return !consultations.Any(c =>
                c.ScheduledAt < slotEnd && c.ScheduledAt.AddMinutes(c.DurationMinutes) > slotStart);
        }

        return slots
            .GroupBy(s => s.Date.Date)
            .Where(g => g.Any(s => IsFreeAndFuture(g.Key, s.Hour)))
            .Select(g => g.Key.Day)
            .OrderBy(d => d)
            .ToList();
    }
}