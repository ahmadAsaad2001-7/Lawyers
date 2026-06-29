using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.InfraStructure.Data;

namespace Lawyers.Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _currentTransaction;
    private IRepository<User> _users;
    private IRepository<ClientProfile> _clientProfiles;
    private IRepository<LawyerProfile> _lawyerProfiles;
    private IRepository<Consultation> _consultations;
    private IRepository<Payment> _payments;
    private IRepository<Message> _messages;

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
    public async Task BeginTransactionAsync()
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_currentTransaction != null) await _currentTransaction.CommitAsync();
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
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // The audit logic (CreatedAt, CreatedByUserId, etc.) will be added here on Day 4
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}