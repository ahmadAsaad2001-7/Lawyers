using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Posts.Commands; // ✅ fixed: Posts, not Topics — matches folder

// ✅ LawyerId removed — always derived server-side from the authenticated caller
public record CreateLawyerPostCommand(
    string Title,
    string Content,
    string? Excerpt,
    PostType Type,
    string? CoverImageUrl,
    List<string>? AttachmentUrls,
    bool IsFeatured
) : IRequest<int>;

public class CreateLawyerPostCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<CreateLawyerPostCommand, int>
{
    public async Task<int> Handle(CreateLawyerPostCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var lawyerProfile = await unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == userId, cancellationToken);

        if (lawyerProfile == null)
            throw new UnauthorizedAccessException("Only lawyers can create posts.");

        if (!lawyerProfile.IsVerified)
            throw new UnauthorizedAccessException("Only verified lawyers can publish posts.");

        var post = new LawyerPost
        {
            LawyerId = lawyerProfile.Id, // ✅ derived, never client-supplied
            Title = request.Title,
            Content = request.Content,
            Excerpt = request.Excerpt ?? (request.Content.Length > 150 ? request.Content[..150] + "..." : request.Content),
            Type = request.Type,
            CoverImageUrl = request.CoverImageUrl,
            IsFeatured = request.IsFeatured,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow,
        };

        if (request.AttachmentUrls is { Count: > 0 })
        {
            foreach (var url in request.AttachmentUrls)
            {
                post.Attachments.Add(new PostAttachment { FileUrl = url, FileType = "Image" });
            }
        }

        await unitOfWork.LawyerPosts.AddAsync(post, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return post.Id;
    }
}