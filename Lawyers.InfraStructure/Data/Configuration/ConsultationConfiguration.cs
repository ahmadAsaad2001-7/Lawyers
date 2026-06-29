using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configurations;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("Consultations");

        builder.HasKey(c => c.Id);

        // BaseEntity properties
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);
        builder.Property(c => c.RowVersion).IsRowVersion();

        // AuditableEntity properties
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedByUserId).IsRequired(false);
        builder.Property(c => c.LastModifiedAt).IsRequired(false);
        builder.Property(c => c.LastModifiedByUserId).IsRequired(false);
        builder.Property(c => c.DeletedAt).IsRequired(false);
        builder.Property(c => c.DeletedByUserId).IsRequired(false);

        // Specific properties
        builder.Property(c => c.ScheduledAt).IsRequired();
        builder.Property(c => c.DurationMinutes).IsRequired();
        
        // Store Enum as string in DB
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Relationships
        builder.HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting client if they have consultations

        builder.HasOne(c => c.Lawyer)
            .WithMany()
            .HasForeignKey(c => c.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1-to-1 (or 1-to-0..1) with Payment. 
        // Consultation is the dependent because it holds the PaymentId FK.
        builder.HasOne(c => c.Payment)
            .WithOne(p => p.Consultation)
            .HasForeignKey<Payment>(p => p.ConsultationId) // <--- FK is on Payment!
            .IsRequired(false) // A consultation might be created before payment is attempted
            .OnDelete(DeleteBehavior.Cascade); 
    }
}