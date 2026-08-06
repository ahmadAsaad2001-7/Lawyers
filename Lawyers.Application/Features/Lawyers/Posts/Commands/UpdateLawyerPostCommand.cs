using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Topics.Commands;
public record UpdateLawyerPostCommand(
    int Id,
    int LawyerId,
    string Title,
    string Content,
    string? Excerpt,
    PostType Type,
    string? CoverImageUrl,
    bool IsFeatured
) : IRequest<bool>;

public class UpdateLawyerPostCommandHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<UpdateLawyerPostCommand, bool>
{
    public async Task<bool> Handle(UpdateLawyerPostCommand request, CancellationToken cancellationToken)
    {var post = await unitOfWork.LawyerPosts.Query()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.LawyerId == request.LawyerId, cancellationToken);

        if (post == null) return false;

        post.Title = request.Title;
        post.Content = request.Content;
        post.Excerpt = request.Excerpt;
        post.Type = request.Type;
        post.CoverImageUrl = request.CoverImageUrl;
        post.IsFeatured = request.IsFeatured;

        unitOfWork.LawyerPosts.Update(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}