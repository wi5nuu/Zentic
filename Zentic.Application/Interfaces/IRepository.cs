using Zentic.Domain.Entities;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
        IQueryable<T> Query();
    }
}
