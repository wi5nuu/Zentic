using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    /// <summary>
    /// Timeline approval per requisition — immutable history
    /// </summary>
    public class RequisitionApprovalHistory
    {
        public long Id { get; set; }
        public int RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }

        public int ActorId { get; set; }
        public AppUser? Actor { get; set; }

        public ApprovalAction Action { get; set; }
        public RequisitionStatus FromStatus { get; set; }
        public RequisitionStatus ToStatus { get; set; }
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
