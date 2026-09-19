using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Profile.Queries;

public record GetProfileOverviewQuery(int UserId) : IRequest<ProfileOverviewDto>;

public class GetProfileOverviewQueryHandler : IRequestHandler<GetProfileOverviewQuery, ProfileOverviewDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProfileOverviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProfileOverviewDto> Handle(GetProfileOverviewQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.LawyerProfile)
            .Include(u => u.ClientProfile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) throw new Exception("User not found");

        var isLawyer = user.LawyerProfile != null;
        var profileId = isLawyer ? user.LawyerProfile!.Id : user.ClientProfile!.Id;

        // 1. Total Consultations
        var totalConsultations = await _unitOfWork.Consultations.Query()
            .CountAsync(c => (isLawyer ? c.LawyerId : c.ClientId) == profileId, cancellationToken);

        // 2. Total Earned (Lawyers only)
        decimal totalEarned = 0;
        if (isLawyer)
        {
            totalEarned = await _unitOfWork.Payments.Query()
                .Where(p => p.LawyerId == profileId && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount, cancellationToken);
        }

        // 3. Average Rating (AverageRating is decimal, not nullable)
        double averageRating = isLawyer ? (double)user.LawyerProfile!.AverageRating : 0.0;

        // 4. Member Since
        var memberSince = user.CreatedAt; 

        // 5. Upcoming Consultations (Next 3) - Fetch first, then project to avoid expression tree issues
        var upcomingConsultations = await _unitOfWork.Consultations.Query()
            .Include(c => c.Client)
            .ThenInclude(cl => cl!.User)
            .Include(c => c.Lawyer)
            .ThenInclude(l => l!.User)
            .Where(c => (isLawyer ? c.LawyerId : c.ClientId) == profileId && 
                        c.ScheduledAt >= DateTime.UtcNow && 
                        c.Status != ConsultationStatus.Cancelled)
            .OrderBy(c => c.ScheduledAt)
            .Take(3)
            .ToListAsync(cancellationToken);

        var upcoming = upcomingConsultations.Select(c => new UpcomingConsultationDto
        {
            Id = c.Id,
            OtherPartyName = isLawyer 
                ? (c.Client?.FullName ?? c.Client?.User?.UserName ?? "Unknown") 
                : (c.Lawyer?.FullName ?? c.Lawyer?.User?.UserName ?? "Unknown"),
            ScheduledAt = c.ScheduledAt,
            DurationMinutes = c.DurationMinutes,
            Status = c.Status.ToString()
        }).ToList();

        // 6. Recent Activities - Fetch first, then project
        var recentConsultationEntities = await _unitOfWork.Consultations.Query()
            .Include(c => c.Client)
            .ThenInclude(cl => cl!.User)
            .Include(c => c.Lawyer)
            .ThenInclude(l => l!.User)
            .Where(c => (isLawyer ? c.LawyerId : c.ClientId) == profileId)
            .OrderByDescending(c => c.ScheduledAt)
            .Take(3)
            .ToListAsync(cancellationToken);

        var recentActivities = recentConsultationEntities.Select(c => new SimpleActivityDto
        {
            Description = isLawyer 
                ? $"استشارة جديدة مع {c.Client?.FullName ?? c.Client?.User?.UserName ?? "Unknown"}" 
                : $"استشارة جديدة مع المحامي {c.Lawyer?.FullName ?? c.Lawyer?.User?.UserName ?? "Unknown"}",
            Timestamp = c.ScheduledAt
        }).ToList();

        return new ProfileOverviewDto
        {
            TotalConsultations = totalConsultations,
            TotalEarned = totalEarned,
            AverageRating = averageRating,
            MemberSince = memberSince,
            UpcomingConsultations = upcoming,
            RecentActivities = recentActivities
        };
    }
}