using System.Linq.Expressions;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Infrastructure.Data.Configuration;
using Lawyers.InfraStructure.Data.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Lawyers.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<ClientProfile> ClientProfiles { get; set; }
    public DbSet<LawyerProfile> LawyerProfiles { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<LawyerPost> LawyerPosts { get; set; }
    public DbSet<PostAttachment> PostAttachments { get; set; }
    public DbSet<FreeConsultationMessage> FreeMessages { get; set; }
    public DbSet<PlatformNotification> PlatformNotifications { get; set; }
    public DbSet<LawyerWeeklySchedule> LawyerWeeklySchedules { get; set; }
    public DbSet<LawyerAvailabilityException> LawyerAvailabilityExceptions { get; set; }
    public DbSet<VoteParticipant> VoteParticipants { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedByUserId = _currentUserService.UserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = DateTime.UtcNow;
                    entry.Entity.LastModifiedByUserId = _currentUserService.UserId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.DeletedByUserId = _currentUserService.UserId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new ClientProfileConfiguration());
        builder.ApplyConfiguration(new ConsultationConfiguration());
        builder.ApplyConfiguration(new PaymentConfiguration());
        builder.ApplyConfiguration(new MessageConfiguration());
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new LawyerProfileConfiguration());
        builder.ApplyConfiguration(new FreeConsultationMessageConfiguration());
        builder.ApplyConfiguration(new LawyerPostConfiguration());
        builder.ApplyConfiguration(new PostAttachmentConfiguration());
        builder.ApplyConfiguration(new PlatformNotificationConfiguration());
        builder.ApplyConfiguration(new UserSuspensionConfiguration());
        builder.ApplyConfiguration(new LawyerAvailabilityConfiguration());
        builder.ApplyConfiguration(new LawyerWeeklyScheduleConfiguration());
        builder.ApplyConfiguration(new LawyerAvailabilityExceptionConfiguration());
        builder.ApplyConfiguration(new VoteParticipantConfiguration());
        builder.ApplyConfiguration(new AdminVoteConfiguration());

        ApplySoftDeleteQueryFilters(builder);

        // ✅ REMOVED: the RowVersion default-value loop that used to run here.
        // SQL Server's `rowversion` type (mapped via [Timestamp]/IsRowVersion())
        // is database-generated on every insert/update and cannot carry an
        // application-supplied DEFAULT constraint — that loop was Postgres-only
        // and would break migration generation against SQL Server.
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.ClrType == null ||
                entityType.FindProperty(nameof(BaseEntity.IsDeleted))?.ClrType != typeof(bool))
            {
                continue;
            }

            var entity = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(entity, nameof(BaseEntity.IsDeleted));
            var notDeleted = Expression.Equal(isDeleted, Expression.Constant(false));

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(notDeleted, entity));
        }
    }
}