using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.LawyerSchedule.Queries;

public record GetActualAvailabilityQuery(int LawyerProfileId, DateTime Date)
    : IRequest<List<int>>;

public class GetActualAvailabilityQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetActualAvailabilityQuery, List<int>>
{
    public async Task<List<int>> Handle(GetActualAvailabilityQuery request, CancellationToken ct)
    {
        var cairo = GetCairoTimeZone();
        // Callers pass a calendar date (year/month/day). Interpret it in Egypt time,
        // not as a UTC instant — weekly hours and the booking picker are local.
        var localDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Unspecified);
        var cairoNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cairo);

        if (localDate.Date < cairoNow.Date)
            return [];

        var dayOfWeek = localDate.DayOfWeek;
        List<int> availableHours = [];

        var exception = await unitOfWork.LawyerAvailabilityExceptions.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.LawyerProfileId == request.LawyerProfileId &&
                e.Date == localDate, ct);

        if (exception != null)
        {
            if (exception.Type == ExceptionType.Closed)
                return [];

            if (exception.Type == ExceptionType.ModifiedHours &&
                exception.StartTime.HasValue && exception.EndTime.HasValue)
            {
                availableHours = HoursInRange(exception.StartTime.Value, exception.EndTime.Value);
            }
        }

        if (availableHours.Count == 0)
        {
            var schedule = await unitOfWork.LawyerWeeklySchedules.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.LawyerProfileId == request.LawyerProfileId &&
                    s.Day == dayOfWeek &&
                    s.IsEnabled, ct);

            if (schedule is { StartTime: not null, EndTime: not null })
                availableHours = HoursInRange(schedule.StartTime.Value, schedule.EndTime.Value);
        }

        if (availableHours.Count == 0)
            return [];

        var utcDayStart = TimeZoneInfo.ConvertTimeToUtc(localDate, cairo);
        var utcDayEnd = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), cairo);

        var bookings = await unitOfWork.Consultations.Query()
            .AsNoTracking()
            .Where(c =>
                c.LawyerId == request.LawyerProfileId &&
                c.Status != ConsultationStatus.Cancelled &&
                c.ScheduledAt < utcDayEnd &&
                c.ScheduledAt.AddMinutes(c.DurationMinutes) > utcDayStart)
            .Select(c => new { c.ScheduledAt, c.DurationMinutes })
            .ToListAsync(ct);

        availableHours.RemoveAll(hour =>
        {
            var slotStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.AddHours(hour), cairo);
            var slotEndUtc = slotStartUtc.AddHours(1);

            return bookings.Any(b =>
            {
                var bookingStart = AsUtc(b.ScheduledAt);
                var bookingEnd = bookingStart.AddMinutes(b.DurationMinutes);
                return bookingStart < slotEndUtc && bookingEnd > slotStartUtc;
            });
        });

        if (localDate.Date == cairoNow.Date)
            availableHours.RemoveAll(hour => hour <= cairoNow.Hour);

        return availableHours;
    }

    private static List<int> HoursInRange(TimeSpan start, TimeSpan end)
    {
        var startHour = (int)Math.Floor(start.TotalHours);
        var endHour = (int)Math.Ceiling(end.TotalHours);
        startHour = Math.Clamp(startHour, 0, 24);
        endHour = Math.Clamp(endHour, 0, 24);
        if (endHour <= startHour)
            return [];
        return Enumerable.Range(startHour, endHour - startHour).ToList();
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static TimeZoneInfo GetCairoTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
    }
}
