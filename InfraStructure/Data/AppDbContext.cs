using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfraStructure.Data;

public class AppDbContext: DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // The Aggregate Roots get DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<LawyerProfile> LawyerProfiles { get; set; }

    // This implements IUnitOfWork
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken); 
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}