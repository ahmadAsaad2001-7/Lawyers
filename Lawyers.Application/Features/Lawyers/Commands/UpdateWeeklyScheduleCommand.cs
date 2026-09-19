using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lawyers.Application.Features.LawyerSchedule.DTOs; 

namespace Lawyers.Application.Features.Lawyers.Commands;



// ✅ Now it uses the DTO version
public record UpdateWeeklyScheduleCommand(
    int LawyerProfileId,
    List<WeeklyScheduleInput> Schedule) : IRequest;



public class UpdateWeeklyScheduleCommandHandler : IRequestHandler<UpdateWeeklyScheduleCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWeeklyScheduleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ✅ Changed return type from Task<Unit> to Task
    public async Task Handle(UpdateWeeklyScheduleCommand request, CancellationToken cancellationToken)
    {
        // 1. Get existing weekly schedules for this lawyer
        var existingSchedules = await _unitOfWork.LawyerWeeklySchedules.Query()
            .Where(s => s.LawyerProfileId == request.LawyerProfileId)
            .ToListAsync(cancellationToken);

        // 2. Delete all existing schedules (we'll recreate them)
        foreach (var schedule in existingSchedules)
        {
            _unitOfWork.LawyerWeeklySchedules.Delete(schedule);
        }

        // 3. Create new schedules from the input
        foreach (var input in request.Schedule)
        {
            if (!input.IsEnabled)
            {
                // Skip disabled days (don't create a record)
                continue;
            }

            var newSchedule = new LawyerWeeklySchedule
            {
                LawyerProfileId = request.LawyerProfileId,
                Day =(DayOfWeek) input.Day,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                IsEnabled = true
            };

            await _unitOfWork.LawyerWeeklySchedules.AddAsync(newSchedule, cancellationToken);
        }

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // ✅ No return statement needed for Task
    }
}