using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class UserSuspensionConfiguration : IEntityTypeConfiguration<UserSuspension>
{
    public void Configure(EntityTypeBuilder<UserSuspension> builder)
    {
        // Table name (optional, defaults to DbSet property name)
        builder.ToTable("UserSuspensions");

        // Primary key (assumes BaseEntity has an int Id)
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500); // adjust length as needed

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.EndsAt)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Relationship: User -> UserSuspensions (one-to-many)
        builder.HasOne(x => x.User)
            .WithMany() // if User doesn't have a collection navigation, use parameterless
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict); // prevents deleting a user with active suspensions

        // Indexes for common queries
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.UserId, x.IsActive }); // composite for quick lookup
    }
}