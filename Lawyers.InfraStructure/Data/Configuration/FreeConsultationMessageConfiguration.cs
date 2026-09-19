using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class FreeConsultationMessageConfiguration : IEntityTypeConfiguration<FreeConsultationMessage>
{
    public void Configure(EntityTypeBuilder<FreeConsultationMessage> builder)
    {
        builder.ToTable("FreeConsultationMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.SenderPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.SenderEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.SenderIpAddress)
            .HasMaxLength(45);

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Foreign key relationship with LawyerProfile
        builder.HasOne(m => m.Lawyer)
            .WithMany()
            .HasForeignKey(m => m.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}