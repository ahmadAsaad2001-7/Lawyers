using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        // BaseEntity properties
        builder.Property(m => m.IsDeleted).HasDefaultValue(false);
        builder.Property(m => m.RowVersion).IsRowVersion();

        // AuditableEntity properties
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.CreatedByUserId).IsRequired(false);
        builder.Property(m => m.LastModifiedAt).IsRequired(false);
        builder.Property(m => m.LastModifiedByUserId).IsRequired(false);
        builder.Property(m => m.DeletedAt).IsRequired(false);
        builder.Property(m => m.DeletedByUserId).IsRequired(false);

        // Specific properties
        builder.Property(m => m.Content).IsRequired().HasMaxLength(4000);

        // Relationships
        builder.HasOne(m => m.Consultation)
            .WithMany()
            .HasForeignKey(m => m.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}