using Zentic.Domain.Enums;
using Zentic.Application.DTOs;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(AuditEventType eventType, string entityName, string entityId,
            int userId, string? ipAddress = null, string? oldValues = null,
            string? newValues = null, string? notes = null, int? requisitionId = null,
            CancellationToken cancellationToken = default);

        Task<PaginatedResult<AuditLogDto>> GetPagedAsync(AuditFilterDto filter, CancellationToken cancellationToken = default);
    }
}
