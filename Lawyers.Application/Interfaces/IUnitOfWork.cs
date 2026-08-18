using Lawyers.Domain.Entities;
using System.Data;

namespace Lawyers.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<ClientProfile> ClientProfiles { get; }
    IRepository<LawyerProfile> LawyerProfiles { get; }
    IRepository<Consultation> Consultations { get; }
    IRepository<Payment> Payments { get; }
    IRepository<Message> Messages { get; }
    IRepository<FreeConsultationMessage> FreeMessages { get; }
    IRepository<LawyerPost> LawyerPosts { get; }
    IRepository<VoteParticipant> VoteParticipants { get; }
    IRepository<AdminVote>  AdminVotes { get; }
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
