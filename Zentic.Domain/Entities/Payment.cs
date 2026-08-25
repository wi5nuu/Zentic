using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public string PaymentNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Currency Currency { get; set; } = Currency.IDR;
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // Payment details
        public DateTime? PaymentDate { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? TransactionId { get; set; }

        // Bank details
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolderName { get; set; }

        // Processing
        public int? ProcessedById { get; set; }
        public AppUser? ProcessedBy { get; set; }
        public string? ProcessingNotes { get; set; }

        // Error handling
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? LastRetryAt { get; set; }

        // Reconciliation
        public bool IsReconciled { get; set; } = false;
        public DateTime? ReconciledAt { get; set; }
        public int? ReconciledById { get; set; }
        public AppUser? ReconciledBy { get; set; }

        // FK
        public int RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }

        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        // Navigation
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
