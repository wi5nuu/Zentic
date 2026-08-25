using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class PurchaseOrder : BaseEntity
    {
        public string PONumber { get; set; } = string.Empty;
        public DateTime PODate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }

        // Amounts
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
        public Currency Currency { get; set; } = Currency.IDR;

        // Status
        public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
        public bool IsFullyReceived { get; set; } = false;
        public DateTime? ReceivedAt { get; set; }

        // Notes
        public string? Notes { get; set; }
        public string? TermsAndConditions { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryInstructions { get; set; }

        // FK
        public int RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }

        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public int CreatedById { get; set; }
        public AppUser? CreatedBy { get; set; }

        public int? ApprovedById { get; set; }
        public AppUser? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Navigation
        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = "pcs";
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } = 0;
        public decimal LineTotal { get; set; }

        public int QuantityReceived { get; set; } = 0;
        public bool IsFullyReceived => QuantityReceived >= Quantity;
    }

    public class GoodsReceipt : BaseEntity
    {
        public string GRNumber { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public bool IsPartialReceipt { get; set; } = false;

        // Quality check
        public bool QualityCheckPassed { get; set; } = true;
        public string? QualityNotes { get; set; }

        // FK
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public int ReceivedById { get; set; }
        public AppUser? ReceivedBy { get; set; }

        // Navigation
        public ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class GoodsReceiptItem
    {
        public int Id { get; set; }
        public int GoodsReceiptId { get; set; }
        public GoodsReceipt? GoodsReceipt { get; set; }

        public int PurchaseOrderItemId { get; set; }
        public PurchaseOrderItem? PurchaseOrderItem { get; set; }

        public int QuantityReceived { get; set; }
        public bool IsDamaged { get; set; } = false;
        public string? DamageNotes { get; set; }
    }
}
