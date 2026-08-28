using Zentic.Domain.Enums;

namespace Zentic.Application.DTOs
{
    // Document DTOs
    public class DocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileSizeDisplay => FormatFileSize(FileSizeBytes);
        public string MimeType { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEncrypted { get; set; }
        public int Version { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class CreateDocumentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DocumentType DocumentType { get; set; }
        public string? Description { get; set; }
        public bool ShouldEncrypt { get; set; } = false;
        public int? RequisitionId { get; set; }
        public int? VendorId { get; set; }
        public int? PaymentId { get; set; }
    }

    // Notification DTOs
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public int RecipientId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? ActionUrl { get; set; }
        public string? TemplateName { get; set; }
    }

    // Payment DTOs
    public class PaymentDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? TransactionId { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountHolderName { get; set; }
        public string? MaskedAccountNumber { get; set; }
        public string? ProcessedByName { get; set; }
        public bool IsReconciled { get; set; }
        public int RequisitionId { get; set; }
        public string RequisitionNumber { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentDto
    {
        public decimal Amount { get; set; }
        public Currency Currency { get; set; } = Currency.IDR;
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolderName { get; set; }
        public string? ProcessingNotes { get; set; }
        public int RequisitionId { get; set; }
        public int VendorId { get; set; }
    }

    // Budget DTOs
    public class BudgetAllocationDto
    {
        public int Id { get; set; }
        public string BudgetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal CommittedAmount { get; set; }
        public decimal AvailableAmount { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public decimal WarningThresholdPercentage { get; set; } = 80;
        public string Currency { get; set; } = string.Empty;
        public string PeriodType { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public int? Quarter { get; set; }
        public int? Month { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public string? Category { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public bool IsOverBudget { get; set; }
        public bool IsNearLimit => UtilizationPercentage >= WarningThresholdPercentage;
    }

    public class CreateBudgetAllocationDto
    {
        public string BudgetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal AllocatedAmount { get; set; }
        public Currency Currency { get; set; } = Currency.IDR;
        public BudgetPeriodType PeriodType { get; set; }
        public int FiscalYear { get; set; }
        public int? Quarter { get; set; }
        public int? Month { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public ExpenseCategory? Category { get; set; }
        public int DepartmentId { get; set; }
        public decimal WarningThresholdPercentage { get; set; } = 80;
    }

    // Purchase Order DTOs
    public class PurchaseOrderDto
    {
        public int Id { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public DateTime PODate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsFullyReceived { get; set; }
        public string? Notes { get; set; }
        public int RequisitionId { get; set; }
        public string RequisitionNumber { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public List<PurchaseOrderItemDto> Items { get; set; } = new();
    }

    public class PurchaseOrderItemDto
    {
        public int Id { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal LineTotal { get; set; }
        public int QuantityReceived { get; set; }
        public bool IsFullyReceived { get; set; }
    }

    public class CreatePurchaseOrderDto
    {
        public DateTime PODate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public Currency Currency { get; set; } = Currency.IDR;
        public string? Notes { get; set; }
        public string? TermsAndConditions { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryInstructions { get; set; }
        public int RequisitionId { get; set; }
        public int VendorId { get; set; }
        public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
    }

    public class CreatePurchaseOrderItemDto
    {
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = "pcs";
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } = 0;
    }

    // Approval Workflow DTOs
    public class ApprovalWorkflowDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Category { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public List<ApprovalStepDto> Steps { get; set; } = new();
    }

    public class ApprovalStepDto
    {
        public int Id { get; set; }
        public int StepOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApproverType { get; set; } = string.Empty;
        public int? SpecificUserId { get; set; }
        public string? SpecificUserName { get; set; }
        public string? RequiredRole { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool AllowDelegation { get; set; }
        public bool RequireAllApprovers { get; set; }
        public int? EscalationRuleId { get; set; }
        public string? EscalationRuleName { get; set; }
        public bool IsParallel { get; set; }
    }

    public class CreateApprovalWorkflowDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public ExpenseCategory? Category { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int Priority { get; set; } = 10;
        public List<CreateApprovalStepDto> Steps { get; set; } = new();
    }

    public class CreateApprovalStepDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ApproverType ApproverType { get; set; } = ApproverType.SpecificUser;
        public int? SpecificUserId { get; set; }
        public UserRole? RequiredRole { get; set; }
        public int? DepartmentId { get; set; }
        public bool AllowDelegation { get; set; } = true;
        public bool RequireAllApprovers { get; set; } = false;
        public int? EscalationRuleId { get; set; }
        public bool IsParallel { get; set; } = false;
    }

    public class EscalationRuleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int EscalationAfterHours { get; set; }
        public string Trigger { get; set; } = string.Empty;
        public int? EscalateToUserId { get; set; }
        public string? EscalateToUserName { get; set; }
        public string? EscalateToRole { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool IsActive { get; set; }
        public bool NotifyOriginalApprover { get; set; }
        public bool NotifyEscalationTarget { get; set; }
        public string? CustomMessage { get; set; }
    }

    public class CreateEscalationRuleDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int EscalationAfterHours { get; set; } = 24;
        public EscalationTrigger Trigger { get; set; } = EscalationTrigger.NoAction;
        public int? EscalateToUserId { get; set; }
        public UserRole? EscalateToRole { get; set; }
        public int? DepartmentId { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool NotifyOriginalApprover { get; set; } = true;
        public bool NotifyEscalationTarget { get; set; } = true;
        public string? CustomMessage { get; set; }
    }

    public class ApprovalDelegationDto
    {
        public int Id { get; set; }
        public int DelegatorId { get; set; }
        public string? DelegatorName { get; set; }
        public int DelegateId { get; set; }
        public string? DelegateName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public decimal? MaxAmount { get; set; }
    }

    public class CreateApprovalDelegationDto
    {
        public int DelegateId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public int? DepartmentId { get; set; }
        public decimal? MaxAmount { get; set; }
    }

    public class ApprovalResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}