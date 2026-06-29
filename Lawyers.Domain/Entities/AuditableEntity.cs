namespace Lawyers.Domain.Entities;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    
    public DateTime? LastModifiedAt { get; set; }
    public int? LastModifiedByUserId { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }
}