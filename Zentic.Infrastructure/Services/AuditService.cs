using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Zentic.Infrastructure.Services
{
    public class AuditService : IAuditService, IHostedService, IDisposable
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuditService> _logger;
        private readonly List<AuditLog> _pendingLogs = new();
        private readonly object _lock = new();
        private Timer? _flushTimer;
        private bool _disposed;

        public AuditService(AppDbContext db, ILogger<AuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LogAsync(AuditEventType eventType, string entityName, string entityId,
            int userId, string? ipAddress = null, string? oldValues = null,
            string? newValues = null, string? notes = null, int? requisitionId = null,
            CancellationToken cancellationToken = default)
        {
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

            lock (_lock)
            {
                _pendingLogs.Add(log);
                
                if (_pendingLogs.Count >= 50)
                {
                    _ = Task.Run(FlushAsync);
                }
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            List<AuditLog> logsToFlush;
            lock (_lock)
            {
                if (_pendingLogs.Count == 0) return;
                logsToFlush = _pendingLogs.ToList();
                _pendingLogs.Clear();
            }

            try
            {
                await _db.AuditLogs.AddRangeAsync(logsToFlush, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush {Count} audit logs", logsToFlush.Count);
                lock (_lock)
                {
                    _pendingLogs.InsertRange(0, logsToFlush);
                }
            }
        }

        public async Task<PaginatedResult<AuditLogDto>> GetPagedAsync(AuditFilterDto filter, CancellationToken cancellationToken = default)
        {
            await FlushAsync(cancellationToken);

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

            var totalCount = await query.CountAsync(cancellationToken);

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
                    ChangedFields = string.IsNullOrEmpty(a.OldValues) && string.IsNullOrEmpty(a.NewValues) ? null : $"{a.OldValues} -> {a.NewValues}"
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _flushTimer = new Timer(async _ => await FlushAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _flushTimer?.Change(Timeout.Infinite, 0);
            await FlushAsync(cancellationToken);
            _flushTimer?.Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _flushTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}
