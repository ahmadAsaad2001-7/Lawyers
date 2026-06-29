using Domain.Common;

namespace Domain.Repositories;

public interface IRepository<T> where T : AggregateRoot<Guid>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
    IUnitOfWork UnitOfWork { get; } // Exposes the saving mechanism
}