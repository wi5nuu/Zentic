using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Zentic.Infrastructure.Data;
using System.Threading;

namespace Zentic.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly AppDbContext _db;
        private readonly DbSet<T> _set;

        public Repository(AppDbContext db)
        {
            _db = db;
            _set = db.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            await _set.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _set.Where(e => !e.IsDeleted).ToListAsync(cancellationToken);

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _set.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _set.Update(entity);
            await Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                _set.Update(entity);
            }
        }

        public IQueryable<T> Query() => _set.Where(e => !e.IsDeleted).AsQueryable();
    }
}
