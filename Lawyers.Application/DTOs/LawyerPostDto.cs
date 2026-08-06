using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Application.DTOs;

public record LawyerPostDto(
    int Id,
    int LawyerId,
    string LawyerName,
    string? LawyerImageUrl,
    string Title,
    string Content,
    string? Excerpt,
    PostType Type,
    string? CoverImageUrl,
    List<string> AttachmentUrls,
    int ViewCount,
    int LikeCount,
    bool IsFeatured,
    DateTime CreatedAt
);