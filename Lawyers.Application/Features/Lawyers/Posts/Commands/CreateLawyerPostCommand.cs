using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;

namespace Lawyers.Application.Features.Lawyers.Topics.Commands;

public record CreateLawyerPostCommand(
    int LawyerId,
    string Title,
    string Content,
    string? Excerpt,
    PostType Type,
    string? CoverImageUrl,
    List<string>? AttachmentUrls,
    bool IsFeatured
) : IRequest<int>;

public class CreateLawyerPostCommandHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<CreateLawyerPostCommand, int>
{
    public async Task<int> Handle(CreateLawyerPostCommand request, CancellationToken cancellationToken)
    {
        var post = new LawyerPost
        {
            LawyerId = request.LawyerId,
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