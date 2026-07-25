using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;

        public AuditService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(AuditEventType eventType, string entityName, string entityId,
            int userId, string? ipAddress = null, string? oldValues = null,
            string? newValues = null, string? notes = null, int? requisitionId = null)
        {
            // AuditLog is append-only — we bypass the repository pattern
            // and insert directly to ensure no accidental update/delete
            var log = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                EventType = eventType,
                EntityName = entityName,
                EntityId = entityId,
                UserId = userId,
                IpAddress = ipAddress,
                OldValues = oldValues,
                NewValues = newValues,
                Notes = notes,
                RequisitionId = requisitionId
            };

            await _db.AuditLogs.AddAsync(log);
            await _db.SaveChangesAsync();
        }

        public async Task<PaginatedResult<AuditLogDto>> GetPagedAsync(AuditFilterDto filter)
        {
            var query = _db.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.EntityName))
                query = query.Where(a => a.EntityName == filter.EntityName);

            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId);

            if (filter.From.HasValue)
                query = query.Where(a => a.Timestamp >= filter.From);

            if (filter.To.HasValue)
                query = query.Where(a => a.Timestamp <= filter.To);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    Timestamp = a.Timestamp,
                    EventType = a.EventType.ToString(),
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    ActorName = a.User != null ? a.User.FullName : "System",
                    IpAddress = a.IpAddress,
                    Notes = a.Notes,
                    ChangedFields = a.ChangedFields
                })
                .ToListAsync();

            return new PaginatedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }
}
