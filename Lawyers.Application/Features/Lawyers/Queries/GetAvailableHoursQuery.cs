using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Queries;

public record GetAvailableHoursQuery(int LawyerProfileId, DateTime Date) : IRequest<List<int>>;

public class GetAvailableHoursQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAvailableHoursQuery, List<int>>
{
    public async Task<List<int>> Handle(GetAvailableHoursQuery q, CancellationToken ct)
    {
        var date = q.Date.Date;

        var slots = await unitOfWork.LawyerAvailabilities.Query().AsNoTracking()
            .Where(a => a.LawyerProfileId == q.LawyerProfileId && a.Date == date)
            .Select(a => a.Hour)
            .ToListAsync(ct);

        if (slots.Count == 0) return new List<int>();

        var dayStart = date;
        var dayEnd = date.AddDays(1);

        var consultations = await unitOfWork.Consultations.Query().AsNoTracking()
            .Where(c => c.LawyerId == q.LawyerProfileId
                        && c.Status != Domain.Entities.Enums.ConsultationStatus.Cancelled
                        && c.ScheduledAt >= dayStart && c.ScheduledAt < dayEnd)
            .Select(c => new { c.ScheduledAt, c.DurationMinutes })
            .ToListAsync(ct);

        return slots
            .Where(h =>
            {
                var slotStart = date.AddHours(h);
                if (slotStart <= DateTime.UtcNow) return false;
                var slotEnd = slotStart.AddHours(1);
                return !consultations.Any(c =>
                    c.ScheduledAt < slotEnd && c.ScheduledAt.AddMinutes(c.DurationMinutes) > slotStart);
            })
            .OrderBy(h => h)
            .ToList();
    }
}