using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Zentic.Infrastructure.Data;

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

        public async Task<T?> GetByIdAsync(int id) =>
            await _set.FirstOrDefaultAsync(e => e.Id == id);

        public async Task<List<T>> GetAllAsync() =>
            await _set.ToListAsync();

        public async Task<T> AddAsync(T entity)
        {
            await _set.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _set.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                _set.Update(entity);
                await _db.SaveChangesAsync();
            }
        }

        public IQueryable<T> Query() => _set.AsQueryable();
    }
}
