using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Topics.Queries;
public record GetLawyerPostsQuery(int LawyerId, int Page = 1, int PageSize = 10) 
    : IRequest<List<LawyerPostSummaryDto>>;

public class GetLawyerPostsQueryHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetLawyerPostsQuery, List<LawyerPostSummaryDto>>
{
    public async Task<List<LawyerPostSummaryDto>> Handle(GetLawyerPostsQuery request, CancellationToken cancellationToken)
    {
        return await unitOfWork.LawyerPosts.Query()
            .AsNoTracking()
            .Where(p => p.LawyerId == request.LawyerId && p.IsPublished)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new LawyerPostSummaryDto(
                p.Id,
                p.Title,
                p.Excerpt,
                p.Type,
                p.CoverImageUrl,
                p.LikeCount,
                p.IsFeatured,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}