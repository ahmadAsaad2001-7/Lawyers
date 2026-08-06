using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Topics.Queries;
public record GetLawyerPostByIdQuery(int Id) : IRequest<LawyerPostDto?>;

public class GetLawyerPostByIdQueryHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetLawyerPostByIdQuery, LawyerPostDto?>
{
    public async Task<LawyerPostDto?> Handle(GetLawyerPostByIdQuery request, CancellationToken cancellationToken)
    {
        return await unitOfWork.LawyerPosts.Query()
            .AsNoTracking()
            .Include(p => p.Lawyer)
            .Include(p => p.Attachments)
            .Where(p => p.Id == request.Id && p.IsPublished)
            .Select(p => new LawyerPostDto(
                p.Id,
                p.LawyerId,
                p.Lawyer != null ? p.Lawyer.FullName : string.Empty,
                p.Lawyer != null ? p.Lawyer.ProfileImageUrl : null,
                p.Title,
                p.Content,
                p.Excerpt,
                p.Type,
                p.CoverImageUrl,
                p.Attachments.Select(a => a.FileUrl).ToList(),
                p.ViewCount,
                p.LikeCount,
                p.IsFeatured,
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
    
}
