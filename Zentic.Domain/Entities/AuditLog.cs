using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    /// <summary>
    /// Audit log append-only — tidak boleh ada UPDATE atau DELETE pada tabel ini.
    /// Mencatat setiap aksi user terhadap data sensitif.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AuditEventType EventType { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? OldValues { get; set; }  // JSON snapshot sebelum
        public string? NewValues { get; set; }  // JSON snapshot sesudah
        public string? ChangedFields { get; set; }
        public int UserId { get; set; }
        public AppUser? User { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Notes { get; set; }

        // FK optional ke requisition
        public int? RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }
    }
}
