using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class ExpenseRequisition : BaseEntity
    {
        public string RequisitionNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public ExpenseCategory Category { get; set; }
        public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
        
        // Timestamps
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        // Approval
        public string? ApprovalNotes { get; set; }
        public string? RejectionReason { get; set; }

        // Anti-fraud fields
        public bool HasConflictOfInterest { get; set; } = false;
        public string? ConflictOfInterestDetails { get; set; }
        public bool IsFlaggedForReview { get; set; } = false;
        public string? FlagReason { get; set; }
        public decimal? BenchmarkPrice { get; set; }
        public decimal? PriceDeviation { get; set; }

        // Supporting documents
        public string? DocumentPath { get; set; }

        // FK
        public int RequesterId { get; set; }
        public AppUser? Requester { get; set; }

        public int? ApproverId { get; set; }
        public AppUser? Approver { get; set; }

        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        // Navigation
        public ICollection<RequisitionQuotation> Quotations { get; set; } = new List<RequisitionQuotation>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<RequisitionApprovalHistory> ApprovalHistory { get; set; } = new List<RequisitionApprovalHistory>();
    }
}
