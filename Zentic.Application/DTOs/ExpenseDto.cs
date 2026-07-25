using Zentic.Domain.Enums;

namespace Zentic.Application.DTOs
{
    public class ExpenseRequisitionDto
    {
        public int Id { get; set; }
        public int RequesterId { get; set; }
        public string RequisitionNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public bool IsFlaggedForReview { get; set; }
        public string? FlagReason { get; set; }
        public decimal? PriceDeviation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? ApprovalNotes { get; set; }
        public string? RejectionReason { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public string? ApproverName { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();
    }

    public class CreateExpenseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int? VendorId { get; set; }
        public bool HasConflictOfInterest { get; set; } = false;
        public string? ConflictOfInterestDetails { get; set; }
    }

    public class ExpenseFilterDto
    {
        public string? Status { get; set; }
        public string? Search { get; set; }
        public string? Category { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ApprovalHistoryDto
    {
        public string ActorName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PriceCheckResult
    {
        public bool IsFlagged { get; set; }
        public decimal? BenchmarkPrice { get; set; }
        public decimal? DeviationPercent { get; set; }
        public string? Message { get; set; }
        public bool IsStructuringPattern { get; set; }
    }
}
