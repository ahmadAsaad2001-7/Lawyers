using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lawyers.InfraStructure.Data.Configuration;

public class VoteParticipantConfiguration : IEntityTypeConfiguration<VoteParticipant>
{
    public void Configure(EntityTypeBuilder<VoteParticipant> builder)
    {
        builder.ToTable("VoteParticipants");
        builder.HasKey(vp => vp.Id);

        builder.Property(vp => vp.RowVersion).IsRowVersion();
        builder.Property(vp => vp.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(vp => !vp.IsDeleted);

        builder.Property(vp => vp.CreatedAt).IsRequired();
        builder.Property(vp => vp.CreatedByUserId).IsRequired(false);
        builder.Property(vp => vp.LastModifiedAt).IsRequired(false);
        builder.Property(vp => vp.LastModifiedByUserId).IsRequired(false);
        builder.Property(vp => vp.DeletedAt).IsRequired(false);
        builder.Property(vp => vp.DeletedByUserId).IsRequired(false);

        builder.Property(vp => vp.IsApproved).IsRequired();
        builder.Property(vp => vp.VotedAt).IsRequired();

        builder.HasOne(vp => vp.AdminVote)
            .WithMany(av => av.Participants)
            .HasForeignKey(vp => vp.AdminVoteId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ FIXED: was implicitly Cascade (no OnDelete specified on a
        // required FK). Now explicitly Restrict, matching every other
        // User-pointing FK in AdminVote/VoteParticipant — deleting an
        // admin should be blocked, not silently wipe their vote history.
        builder.HasOne(vp => vp.AdminUser)
            .WithMany()
            .HasForeignKey(vp => vp.AdminUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(vp => new { vp.AdminVoteId, vp.AdminUserId })
            .IsUnique();
    }
}