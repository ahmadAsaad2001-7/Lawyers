using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetDashBoardStatsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetDashBoardStatsQuery, DashBoardStatsDto>
{
    public async Task<DashBoardStatsDto> Handle(GetDashBoardStatsQuery request, CancellationToken ct)
    {
        var totalUsers = await unitOfWork.Users.Query().CountAsync(ct);
        var totalRevenue = await unitOfWork.Payments.Query()
            .Where(payment => payment.Status == PaymentStatus.Succeeded)
            .SumAsync(payment => (decimal?)payment.Amount, ct) ?? 0m;
        var activeConsultations = await unitOfWork.Consultations.Query()
            .CountAsync(consultation =>
                consultation.Status == ConsultationStatus.Confirmed ||
                consultation.Status == ConsultationStatus.InProgress, ct);
        var pendingVerifications = await unitOfWork.LawyerProfiles.Query()
            .CountAsync(profile => !profile.IsVerified, ct);

        return new DashBoardStatsDto
        {
            TotalUsers = totalUsers,
            TotalRevenue = totalRevenue,
            ActiveConsultations = activeConsultations,
            PendingVerifications = pendingVerifications
        };
    }
}
