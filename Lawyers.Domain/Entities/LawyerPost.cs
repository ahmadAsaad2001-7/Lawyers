using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Domain.Entities;

public class LawyerPost : AuditableEntity
{
    public int LawyerId { get; set; } 
    public LawyerProfile Lawyer { get; set; } = null!;

    // Post Content
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;       // Supports rich text / HTML
    public string? Excerpt { get; set; }                       // Short snippet for feed preview
    public PostType Type { get; set; } = PostType.GeneralInsight;

    // Media & Attachments
    public string? CoverImageUrl { get; set; }                 // Main header/featured image
    public ICollection<PostAttachment> Attachments { get; set; } = new List<PostAttachment>(); 
    // ^ E.g., Certificates, press releases, verdict PDFs, images
    

    // Engagement Metrics (Denormalized counters for fast rendering)
    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;

    // Visibility & Status
    public bool IsPublished { get; set; } = true;              // Draft vs. Live
    public bool IsFeatured { get; set; } = false;              // Pin to top of lawyer's profile
    public DateTime? PublishedAt { get; set; }
}

public class PostAttachment
{
    public int Id { get; set; }
    public int LawyerPostId { get; set; }
    public LawyerPost LawyerPost { get; set; } = null!;   // Add navigation back to post
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "Image", "PDF", etc.
}
