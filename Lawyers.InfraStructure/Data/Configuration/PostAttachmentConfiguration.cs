using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class PostAttachmentConfiguration : IEntityTypeConfiguration<PostAttachment>
{
    public void Configure(EntityTypeBuilder<PostAttachment> builder)
    {
        builder.ToTable("PostAttachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(a => a.FileType)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(a => a.LawyerPost)
            .WithMany(p => p.Attachments)
            .HasForeignKey(a => a.LawyerPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}