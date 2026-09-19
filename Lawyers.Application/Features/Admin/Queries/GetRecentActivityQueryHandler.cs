using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetRecentActivityQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRecentActivityQuery, List<RecentActivityDto>>
{
    public Task<List<RecentActivityDto>> Handle(GetRecentActivityQuery request, CancellationToken ct) =>
        unitOfWork.Consultations.Query()
            .AsNoTracking()
            .OrderByDescending(consultation => consultation.CreatedAt)
            .Take(request.Limit)
            .Select(consultation => new RecentActivityDto
            {
                Id = consultation.Id,
                ClientName = consultation.Client.FullName,
                LawyerName = consultation.Lawyer.FullName,
                Amount = consultation.Payment != null ? consultation.Payment.Amount : 0,
                Date = consultation.CreatedAt
            })
            .ToListAsync(ct);
}
