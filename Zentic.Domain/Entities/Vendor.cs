using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class Vendor : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? TaxId { get; set; }
        public VendorStatus Status { get; set; } = VendorStatus.PendingVerification;

        // Contact person
        public string? ContactPersonName { get; set; }
        public string? ContactPersonPhone { get; set; }
        public string? ContactPersonEmail { get; set; }
        public string? ContactPersonPosition { get; set; }

        // Bank account (encrypted at-rest)
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; } // Encrypted
        public string? BankAccountHolderName { get; set; }

        // Payment terms
        public int PaymentTermDays { get; set; } = 30; // NET 30 default
        public string? PaymentTermNotes { get; set; }

        // Rating & Performance
        public decimal AverageRating { get; set; } = 0;
        public int TotalTransactions { get; set; } = 0;
        public decimal TotalTransactionValue { get; set; } = 0;

        // Verification
        public int? VerifiedById { get; set; }
        public AppUser? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerificationNotes { get; set; }

        // Blacklist management
        public DateTime? BlacklistedAt { get; set; }
        public string? BlacklistReason { get; set; }
        public int? BlacklistedById { get; set; }
        public AppUser? BlacklistedBy { get; set; }

        // Navigation
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
        public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    }
}
