using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class LawyerPostConfiguration : IEntityTypeConfiguration<LawyerPost>
{
    public void Configure(EntityTypeBuilder<LawyerPost> builder)
    {
        builder.ToTable("LawyerPosts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)"); // or "text" depending on DB

        builder.Property(p => p.Excerpt)
            .HasMaxLength(500);

        builder.Property(p => p.CoverImageUrl)
            .HasMaxLength(2048);

        // Relationship with LawyerProfile
        builder.HasOne(p => p.Lawyer)
            .WithMany(l => l.Posts)
            .HasForeignKey(p => p.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.LawyerId);
        builder.HasIndex(p => p.PublishedAt).IsDescending();
        builder.HasIndex(p => p.IsFeatured);

        // Global soft-delete filter (inherited from AuditableEntity)
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}