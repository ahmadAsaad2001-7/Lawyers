
using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration; // ✅ FIXED NAMESPACE
public class LawyerAvailabilityExceptionConfiguration : IEntityTypeConfiguration<LawyerAvailabilityException>
{
    public void Configure(EntityTypeBuilder<LawyerAvailabilityException> builder)
    {
        // 1. Table Name
        builder.ToTable("LawyerAvailabilityExceptions");

        // 2. Primary Key
        builder.HasKey(e => e.Id);

        // 3. Property Constraints
        builder.Property(e => e.Date)
            .IsRequired()
            .HasColumnType("date"); // Stores only the date part

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>(); // Store ExceptionType enum as integer

        builder.Property(e => e.StartTime)
            .IsRequired(false)
            .HasColumnType("time");

        builder.Property(e => e.EndTime)
            .IsRequired(false)
            .HasColumnType("time");

        builder.Property(e => e.Reason)
            .IsRequired(false)
            .HasMaxLength(255); // e.g., "National Holiday", "Vacation"

        builder.Property(e => e.ValidFrom)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(e => e.ValidTo)
            .IsRequired(false)
            .HasColumnType("date"); // Null means the exception applies only to the single 'Date'

        // 4. Relationships
        // A LawyerProfile has many exceptions.
        builder.HasOne(e => e.LawyerProfile)
            .WithMany(lp => lp.AvailabilityExceptions) // Requires adding this navigation property to LawyerProfile
            .HasForeignKey(e => e.LawyerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // 5. Indexes (CRITICAL FOR PERFORMANCE)
        // Optimized index for the handler to quickly check: "Does Lawyer X have an exception on Date Y?"
        builder.HasIndex(e => new { e.LawyerProfileId, e.Date })
            .HasDatabaseName("IX_LawyerAvailabilityExceptions_Lawyer_Date");
    }
}