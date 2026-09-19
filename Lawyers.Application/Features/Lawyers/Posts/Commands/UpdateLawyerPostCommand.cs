using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Posts.Commands;

// ✅ LawyerId removed — ownership check happens server-side
public record UpdateLawyerPostCommand(
    int Id,
    string Title,
    string Content,
    string? Excerpt,
    PostType Type,
    string? CoverImageUrl,
    bool IsFeatured
) : IRequest<bool>;

public class UpdateLawyerPostCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<UpdateLawyerPostCommand, bool>
{
    public async Task<bool> Handle(UpdateLawyerPostCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var lawyerProfile = await unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == userId, cancellationToken);

        if (lawyerProfile == null)
            throw new UnauthorizedAccessException("Only lawyers can edit posts.");

        // ✅ ownership enforced by LawyerId derived from the caller, not the request body
        var post = await unitOfWork.LawyerPosts.Query()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.LawyerId == lawyerProfile.Id, cancellationToken);

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