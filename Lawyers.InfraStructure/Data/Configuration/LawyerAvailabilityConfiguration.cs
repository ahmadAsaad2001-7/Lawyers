using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class LawyerAvailabilityConfiguration : IEntityTypeConfiguration<LawyerAvailability>
{
    public void Configure(EntityTypeBuilder<LawyerAvailability> builder)
    {
        // ✅ Check constraint now lives inside ToTable — the standalone
        // HasCheckConstraint(...) call is obsolete on EF Core 8+, and the
        // identifier quoting changes from "double quotes" (Postgres) to
        // [brackets] (SQL Server).
        builder.ToTable("LawyerAvailabilities", t =>
            t.HasCheckConstraint("CK_LawyerAvailabilities_Hour", "[Hour] >= 0 AND [Hour] <= 23"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Date).IsRequired().HasColumnType("date");
        builder.Property(a => a.Hour).IsRequired();

        builder.HasOne(a => a.LawyerProfile)
            .WithMany()
            .HasForeignKey(a => a.LawyerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.LawyerProfileId, a.Date, a.Hour })
            .IsUnique()
            .HasDatabaseName("IX_LawyerAvailabilities_Lawyer_Date_Hour");
    }
}