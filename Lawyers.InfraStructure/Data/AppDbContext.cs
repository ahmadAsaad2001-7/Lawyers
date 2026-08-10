using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Infrastructure.Data.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Lawyers.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options,ICurrentUserService currentUserService) : base(options)
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
    public DbSet<FreeConsultationMessage> FreeMessages { get; set; } // 👈 1. Added DbSet

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
                    // Intercept delete and turn it into a soft delete
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
        builder.ApplyConfiguration(new FreeConsultationMessageConfiguration()); // 👈 2. Applied Configuration
        builder.ApplyConfiguration(new LawyerPostConfiguration());
        builder.ApplyConfiguration(new PostAttachmentConfiguration());
        var defaultRowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
    
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var rowVersionProperty = entityType.FindProperty("RowVersion");
            if (rowVersionProperty != null && rowVersionProperty.ClrType == typeof(byte[]))
            {
                rowVersionProperty.SetDefaultValue(defaultRowVersion);
            }
        }
    }
}


