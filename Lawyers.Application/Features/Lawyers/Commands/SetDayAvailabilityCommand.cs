using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Commands;

public record SetDayAvailabilityCommand(DateTime Date, List<int> Hours) : IRequest;

public class SetDayAvailabilityCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<SetDayAvailabilityCommand>
{
    public async Task Handle(SetDayAvailabilityCommand cmd, CancellationToken ct)
    {
        var lawyer = await unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == currentUser.UserId!.Value, ct);
        if (lawyer == null) throw new InvalidOperationException("Lawyer profile not found.");

        var date = cmd.Date.Date;

        var existing = await unitOfWork.LawyerAvailabilities.Query()
            .Where(a => a.LawyerProfileId == lawyer.Id && a.Date == date)
            .ToListAsync(ct);
        foreach (var e in existing) unitOfWork.LawyerAvailabilities.Delete(e);

        foreach (var h in cmd.Hours.Distinct().Where(h => h is >= 0 and <= 23))
        {
            await unitOfWork.LawyerAvailabilities.AddAsync(new LawyerAvailability
            {
                LawyerProfileId = lawyer.Id, Date = date, Hour = h
            }, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}