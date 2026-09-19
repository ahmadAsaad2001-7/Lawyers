using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.InfraStructure.Data.Configuration;

public class AdminVoteConfiguration : IEntityTypeConfiguration<AdminVote>
{
    public void Configure(EntityTypeBuilder<AdminVote> builder)
    {
        builder.ToTable("AdminVotes");
        builder.HasKey(av => av.Id);

        builder.Property(av => av.RowVersion).IsRowVersion();
        builder.Property(av => av.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(av => !av.IsDeleted);

        builder.Property(av => av.CreatedAt).IsRequired();
        builder.Property(av => av.CreatedByUserId).IsRequired(false);
        builder.Property(av => av.LastModifiedAt).IsRequired(false);
        builder.Property(av => av.LastModifiedByUserId).IsRequired(false);
        builder.Property(av => av.DeletedAt).IsRequired(false);
        builder.Property(av => av.DeletedByUserId).IsRequired(false);

        builder.Property(av => av.ActionType).IsRequired();
        builder.Property(av => av.Reason).IsRequired();

        builder.Property(av => av.InitiatorAdminId).IsRequired(false);
        builder.Property(av => av.AppliedByUserId).IsRequired(false);

        builder.HasOne(av => av.InitiatorAdmin)
            .WithMany()
            .HasForeignKey(av => av.InitiatorAdminId)
            .IsRequired(false)                     
            .OnDelete(DeleteBehavior.Restrict);     

        builder.HasOne(av => av.AppliedByUser)
            .WithMany()
            .HasForeignKey(av => av.AppliedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(av => av.TargetUser).WithMany().HasForeignKey(av => av.TargetUserId)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}