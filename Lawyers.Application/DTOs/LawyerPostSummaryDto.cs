using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Application.DTOs;

public record LawyerPostSummaryDto(
    int Id,
    string Title,
    string? Excerpt,
    PostType Type,
    string? CoverImageUrl,
    int LikeCount,
    bool IsFeatured,
    DateTime CreatedAt
);