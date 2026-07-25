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

        // Bank account (encrypted at-rest)
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; } // Will be encrypted
        public string? BankAccountHolderName { get; set; }

        // Verification
        public int? VerifiedById { get; set; }
        public AppUser? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }

        // Navigation
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
        public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    }
}
