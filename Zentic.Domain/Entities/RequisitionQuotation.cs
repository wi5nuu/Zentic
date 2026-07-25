namespace Zentic.Domain.Entities
{
    /// <summary>
    /// Multiple quotation untuk requisition di atas threshold tertentu
    /// </summary>
    public class RequisitionQuotation : BaseEntity
    {
        public int RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }

        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public decimal QuotedPrice { get; set; }
        public string? QuotationDocumentPath { get; set; }
        public DateTime QuotationDate { get; set; }
        public bool IsSelected { get; set; } = false;
        public string? SelectionReason { get; set; }
    }
}
