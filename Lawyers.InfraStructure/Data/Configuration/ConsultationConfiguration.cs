using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("Consultations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.IsDeleted).HasDefaultValue(false);
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedByUserId).IsRequired(false);
        builder.Property(c => c.LastModifiedAt).IsRequired(false);
        builder.Property(c => c.LastModifiedByUserId).IsRequired(false);
        builder.Property(c => c.DeletedAt).IsRequired(false);
        builder.Property(c => c.DeletedByUserId).IsRequired(false);

        builder.Property(c => c.ScheduledAt).IsRequired();
        builder.Property(c => c.DurationMinutes).IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // ✅ REMOVED: the duplicate no-collection HasOne(...).WithMany() calls
        // for Client and Lawyer that used to precede these — they were dead
        // (immediately overridden by the calls below) and just noise/risk.
        builder.HasOne(c => c.Client)
            .WithMany(cp => cp.Consultations)
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Lawyer)
            .WithMany(l => l.Consultations)
            .HasForeignKey(c => c.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Payment)
            .WithOne(p => p.Consultation)
            .HasForeignKey<Payment>(p => p.ConsultationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}