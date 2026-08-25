using Zentic.Domain.Enums;

namespace Zentic.Application.DTOs
{
    public class AppUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public bool EmailVerified { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public AppUserDto? User { get; set; }
    }

    public class VendorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? TaxId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? BankAccountHolderName { get; set; }
        public string? MaskedAccountNumber { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class CreateVendorDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? TaxId { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolderName { get; set; }
    }

    public class DashboardDto
    {
        public int TotalRequisitions { get; set; }
        public int PendingApproval { get; set; }
        public int ApprovedThisMonth { get; set; }
        public int RejectedThisMonth { get; set; }
        public int FlaggedForReview { get; set; }
        public decimal TotalAmountThisMonth { get; set; }
        public decimal TotalAmountApproved { get; set; }
        public List<RequisitionSummaryDto> RecentRequisitions { get; set; } = new();
        public List<CategorySummaryDto> CategoryBreakdown { get; set; } = new();
    }

    public class RequisitionSummaryDto
    {
        public int Id { get; set; }
        public string RequisitionNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public bool IsFlagged { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RequesterName { get; set; } = string.Empty;
    }

    public class CategorySummaryDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class AuditLogDto
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string ActorName { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? Notes { get; set; }
        public string? ChangedFields { get; set; }
    }

    public class AuditFilterDto
    {
        public string? EntityName { get; set; }
        public int? UserId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class MfaSetupDto
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeUri { get; set; } = string.Empty;
        public List<string> BackupCodes { get; set; } = new();
        public string ManualEntryKey { get; set; } = string.Empty;
    }
}
