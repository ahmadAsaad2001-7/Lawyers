using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configurations;

public class ClientProfileConfiguration : IEntityTypeConfiguration<ClientProfile>
{
    public void Configure(EntityTypeBuilder<ClientProfile> builder)
    {
        builder.ToTable("ClientProfiles");

        builder.HasKey(cp => cp.Id);

        // BaseEntity properties
        builder.Property(cp => cp.IsDeleted).HasDefaultValue(false);
        builder.Property(cp => cp.RowVersion).IsRowVersion();

        // AuditableEntity properties
        builder.Property(cp => cp.CreatedAt).IsRequired();
        builder.Property(cp => cp.CreatedByUserId).IsRequired(false);
        builder.Property(cp => cp.LastModifiedAt).IsRequired(false);
        builder.Property(cp => cp.LastModifiedByUserId).IsRequired(false);
        builder.Property(cp => cp.DeletedAt).IsRequired(false);
        builder.Property(cp => cp.DeletedByUserId).IsRequired(false);

        // Specific properties
        builder.Property(cp => cp.FullName).IsRequired().HasMaxLength(200);
        builder.Property(cp => cp.PhoneNumber).HasMaxLength(20);
        
        // Note: The principal end of the 1:1 relationship with User is configured in UserConfiguration
    }
}