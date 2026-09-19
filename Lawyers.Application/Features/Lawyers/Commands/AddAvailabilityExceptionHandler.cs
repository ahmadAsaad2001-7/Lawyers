using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Commands;

public class AddAvailabilityExceptionCommandHandler : IRequestHandler<AddAvailabilityExceptionCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService; // ✅ Added for authorization

    public AddAvailabilityExceptionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AddAvailabilityExceptionCommand request, CancellationToken cancellationToken)
    {
        // 1. ✅ Authorization: Ensure the user is modifying their own profile (or is an Admin)
        var lawyerProfile = await _unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.Id == request.LawyerProfileId, cancellationToken);

        if (lawyerProfile == null)
            throw new InvalidOperationException("Lawyer profile not found.");

        if (lawyerProfile.UserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
            throw new UnauthorizedAccessException("You are not authorized to modify this lawyer's schedule.");

        // 2. ✅ Validation: Basic sanity checks
        var targetDate = request.Date.Date;
        if (targetDate < DateTime.Today)
            throw new InvalidOperationException("Cannot add exceptions for past dates.");

        if (request.StartTime >= request.EndTime)
            throw new InvalidOperationException("Start time must be before end time.");

        if (!Enum.IsDefined(typeof(ExceptionType), request.Type))
            throw new ArgumentException("Invalid exception type.");

        // 3. ✅ Overlap Check: Prevent duplicate exceptions for the same time frame
        var existingException = await _unitOfWork.LawyerAvailabilityExceptions.Query()
            .AnyAsync(e => e.LawyerProfileId == request.LawyerProfileId
                        && e.Date == targetDate
                        && e.StartTime < request.EndTime
                        && e.EndTime > request.StartTime, cancellationToken);

        if (existingException)
            throw new InvalidOperationException("An exception already exists for this time frame.");

        // 4. ✅ Create and Save
        var exception = new LawyerAvailabilityException
        {
            LawyerProfileId = request.LawyerProfileId,
            Date = targetDate,
            Type = (ExceptionType)request.Type,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Reason = request.Reason,
            ValidFrom = targetDate,
            ValidTo = null // Single day exception
        };

        await _unitOfWork.LawyerAvailabilityExceptions.AddAsync(exception, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}