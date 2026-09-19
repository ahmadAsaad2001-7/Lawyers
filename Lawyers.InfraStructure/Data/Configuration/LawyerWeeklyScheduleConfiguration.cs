
using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class LawyerWeeklyScheduleConfiguration : IEntityTypeConfiguration<LawyerWeeklySchedule>
{
    public void Configure(EntityTypeBuilder<LawyerWeeklySchedule> builder)
    {
        // 1. Table Name
        builder.ToTable("LawyerWeeklySchedules");

        // 2. Primary Key
        builder.HasKey(s => s.Id);

        // 3. Property Constraints
        builder.Property(s => s.Day)
            .IsRequired()
            .HasConversion<int>(); // Store DayOfWeek enum as integer for better performance

        builder.Property(s => s.StartTime)
            .IsRequired(false)
            .HasColumnType("time");

        builder.Property(s => s.EndTime)
            .IsRequired(false)
            .HasColumnType("time");

        builder.Property(s => s.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        // 4. Relationships
        // A LawyerProfile has many weekly schedule rules.
        // OnDelete.Cascade ensures if a lawyer is deleted, their schedule rules are also deleted.
        builder.HasOne(s => s.LawyerProfile)
            .WithMany(lp => lp.WeeklySchedules) // Requires adding this navigation property to LawyerProfile
            .HasForeignKey(s => s.LawyerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // 5. Indexes (CRITICAL FOR PERFORMANCE)
        // Unique index prevents a lawyer from having two conflicting rules for the same day of the week 
        // (e.g., two different "Monday" schedules).
        builder.HasIndex(s => new { s.LawyerProfileId, s.Day })
            .IsUnique()
            .HasDatabaseName("IX_LawyerWeeklySchedules_Lawyer_Day");
    }
}