using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Queries;

public record GetWeeklyScheduleQuery(int LawyerProfileId) : IRequest<List<WeeklyScheduleDto>>;

// ✅ Updated DTO to match frontend expectations (int instead of DayOfWeek enum)
public class WeeklyScheduleDto
{
    public int Id { get; set; }
    public int Day { get; set; } // Frontend expects an int (0-6)
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool IsEnabled { get; set; }
}

// ✅ ADDED: The Missing Handler Class
public class GetWeeklyScheduleQueryHandler : IRequestHandler<GetWeeklyScheduleQuery, List<WeeklyScheduleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWeeklyScheduleQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<WeeklyScheduleDto>> Handle(GetWeeklyScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedules = await _unitOfWork.LawyerWeeklySchedules.Query()
            .AsNoTracking()
            .Where(s => s.LawyerProfileId == request.LawyerProfileId)
            .Select(s => new WeeklyScheduleDto
            {
                Id = s.Id,
                Day = (int)s.Day, // ✅ Cast enum to int for frontend
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsEnabled = s.IsEnabled
            })
            .ToListAsync(cancellationToken);

        return schedules;
    }
}