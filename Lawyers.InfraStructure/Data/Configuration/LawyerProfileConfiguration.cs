using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.Infrastructure.Data.Configuration;

public class LawyerProfileConfiguration : IEntityTypeConfiguration<LawyerProfile>
{
    public void Configure(EntityTypeBuilder<LawyerProfile> builder)
    {
        builder.ToTable("LawyerProfiles");
        
        builder.HasKey(lp => lp.Id);
        
        // Configure Address as an Owned Entity (Value Object)
        builder.OwnsOne(lp => lp.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
        });
        
        // Other configurations...
        builder.Property(lp => lp.FullName).IsRequired().HasMaxLength(200);
        builder.Property(lp => lp.Bio).HasMaxLength(1000);
        builder.Property(lp => lp.HourlyRate).HasColumnType("decimal(10,2)");
        builder.Property(lp => lp.BarLicenseNumber).HasMaxLength(100);
    }
}