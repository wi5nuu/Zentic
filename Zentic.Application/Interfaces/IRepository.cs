using Zentic.Domain.Entities;

namespace Zentic.Application.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task SoftDeleteAsync(int id);
        IQueryable<T> Query();
    }
}
