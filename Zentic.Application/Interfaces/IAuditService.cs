using Zentic.Domain.Enums;
using Zentic.Application.DTOs;

namespace Zentic.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(AuditEventType eventType, string entityName, string entityId,
            int userId, string? ipAddress = null, string? oldValues = null,
            string? newValues = null, string? notes = null, int? requisitionId = null);

        Task<PaginatedResult<AuditLogDto>> GetPagedAsync(AuditFilterDto filter);
    }
}
