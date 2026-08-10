using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // BaseEntity properties
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
        builder.Property(u => u.RowVersion).IsRowVersion();

        // Specific properties
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();

        // Enum conversion
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        // 1-to-1 Relationships
        builder.HasOne(u => u.ClientProfile)
            .WithOne(cp => cp.User)
            .HasForeignKey<ClientProfile>(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.LawyerProfile)
            .WithOne(lp => lp.User)
            .HasForeignKey<LawyerProfile>(lp => lp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}