using Lawyers.Domain.Entities;

namespace Lawyers.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<ClientProfile> ClientProfiles { get; }
    IRepository<LawyerProfile> LawyerProfiles { get; }
    IRepository<Consultation> Consultations { get; }
    IRepository<Payment> Payments { get; }
    IRepository<Message> Messages { get; }
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}