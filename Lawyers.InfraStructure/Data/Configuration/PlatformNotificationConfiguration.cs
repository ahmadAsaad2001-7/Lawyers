using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class PlatformNotificationConfiguration : IEntityTypeConfiguration<PlatformNotification>
{
    public void Configure(EntityTypeBuilder<PlatformNotification> builder)
    {
        builder.ToTable("PlatformNotifications");

        builder.HasKey(n => n.Id);

        // BaseEntity properties
        builder.Property(n => n.IsDeleted).HasDefaultValue(false);
        builder.Property(n => n.RowVersion).IsRowVersion();

        // AuditableEntity properties
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.CreatedByUserId).IsRequired(false);
        builder.Property(n => n.LastModifiedAt).IsRequired(false);
        builder.Property(n => n.LastModifiedByUserId).IsRequired(false);
        builder.Property(n => n.DeletedAt).IsRequired(false);
        builder.Property(n => n.DeletedByUserId).IsRequired(false);

        // Specific properties
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.IsRead).HasDefaultValue(false);

        // Relationship: notifications belong to a user
        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade); // user hard-deleted → notifications go too

        // ✅ Speeds up the inbox query (recipient + unread filter + date sort)
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt });
    }
}