using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Topics.Queries;

// Query
public record SearchLawyerPostsQuery(
    string? Term,
    PostType? Type,
    int Page = 1,
    int PageSize = 10
) : IRequest<List<LawyerPostSummaryDto>>;

// Handler
public class SearchLawyerPostsQueryHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<SearchLawyerPostsQuery, List<LawyerPostSummaryDto>>
{
    public async Task<List<LawyerPostSummaryDto>> Handle(SearchLawyerPostsQuery request, CancellationToken cancellationToken)
    {
        var query = unitOfWork.LawyerPosts.Query()
            .AsNoTracking()
            .Where(p => p.IsPublished);

        // Keyword filter
        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var term = request.Term.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) || p.Content.ToLower().Contains(term));
        }

        // Post Type filter (e.g., CaseVictory, Achievement)
        if (request.Type.HasValue)
        {
            query = query.Where(p => p.Type == request.Type.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
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