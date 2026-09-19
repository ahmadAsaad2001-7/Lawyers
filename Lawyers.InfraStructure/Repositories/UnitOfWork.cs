using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Lawyers.Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _currentTransaction;
    
    private IRepository<User>? _users;
    private IRepository<ClientProfile>? _clientProfiles;
    private IRepository<LawyerProfile>? _lawyerProfiles;
    private IRepository<Consultation>? _consultations;
    private IRepository<Payment>? _payments;
    private IRepository<Message>? _messages;
    private IRepository<FreeConsultationMessage>? _freeConsultationMessage;
    private IRepository<LawyerPost>? _lawyerPosts;
    private IRepository<VoteParticipant>? _voteParticipants;
    private IRepository<AdminVote>? _adminVotes;
    private IRepository<UserSuspension>? _userSuspensions;
    private IRepository<PlatformNotification>? _platformNotifications;
    private IRepository<LawyerAvailability>? _lawyerAvailabilities;
    
    // ✅ ADDED: Backing fields for the new repositories
    private IRepository<LawyerWeeklySchedule>? _lawyerWeeklySchedules;
    private IRepository<LawyerAvailabilityException>? _lawyerAvailabilityExceptions;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<ClientProfile> ClientProfiles => _clientProfiles ??= new Repository<ClientProfile>(_context);
    public IRepository<LawyerProfile> LawyerProfiles => _lawyerProfiles ??= new Repository<LawyerProfile>(_context);
    public IRepository<Consultation> Consultations => _consultations ??= new Repository<Consultation>(_context);
    public IRepository<Payment> Payments => _payments ??= new Repository<Payment>(_context);
    public IRepository<Message> Messages => _messages ??= new Repository<Message>(_context);
    public IRepository<FreeConsultationMessage> FreeMessages => _freeConsultationMessage ??= new Repository<FreeConsultationMessage>(_context);
    public IRepository<LawyerPost> LawyerPosts => _lawyerPosts ??= new Repository<LawyerPost>(_context);
    public IRepository<VoteParticipant> VoteParticipants => _voteParticipants ??= new Repository<VoteParticipant>(_context);
    public IRepository<AdminVote> AdminVotes => _adminVotes ??= new Repository<AdminVote>(_context);
    public IRepository<PlatformNotification> PlatformNotifications => _platformNotifications ??= new Repository<PlatformNotification>(_context);
    public IRepository<LawyerAvailability> LawyerAvailabilities => _lawyerAvailabilities ??= new Repository<LawyerAvailability>(_context);
    public IRepository<UserSuspension> UserSuspensions => _userSuspensions ??= new Repository<UserSuspension>(_context);
   

    // ✅ FIXED: Properly initialized with lazy loading pattern
    public IRepository<LawyerWeeklySchedule> LawyerWeeklySchedules => 
        _lawyerWeeklySchedules ??= new Repository<LawyerWeeklySchedule>(_context);

    public IRepository<LawyerAvailabilityException> LawyerAvailabilityExceptions => 
        _lawyerAvailabilityExceptions ??= new Repository<LawyerAvailabilityException>(_context);

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A database transaction is already active for this unit of work.");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(isolationLevel);
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("Cannot commit because no database transaction is active.");
            }

            await _context.SaveChangesAsync();
            await _currentTransaction.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
            _context.ChangeTracker.Clear();
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}