using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        // BaseEntity properties
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);
        builder.Property(p => p.RowVersion).IsRowVersion();

        // AuditableEntity properties
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedByUserId).IsRequired(false);
        builder.Property(p => p.LastModifiedAt).IsRequired(false);
        builder.Property(p => p.LastModifiedByUserId).IsRequired(false);
        builder.Property(p => p.DeletedAt).IsRequired(false);
        builder.Property(p => p.DeletedByUserId).IsRequired(false);

        // Money precision
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.TransactionId)
            .HasMaxLength(255);

        // Enum conversion
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Relationships
        builder.HasOne(p => p.Client)
            .WithMany()
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Lawyer)
            .WithMany()
            .HasForeignKey(p => p.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}