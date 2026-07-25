namespace Zentic.Domain.Entities
{
    /// <summary>
    /// Histori harga per vendor + item untuk benchmarking anti-fraud
    /// </summary>
    public class PriceHistory : BaseEntity
    {
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public string ItemDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime PurchaseDate { get; set; }

        public int RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }
    }
}
