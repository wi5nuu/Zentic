using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class Document : BaseEntity
    {
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public long FileSizeBytes { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DocumentType DocumentType { get; set; }
        public string? Description { get; set; }

        // Security
        public bool IsEncrypted { get; set; } = false;
        public string? EncryptionKey { get; set; }
        public string? FileHash { get; set; } // SHA256 for integrity

        // Versioning
        public int Version { get; set; } = 1;
        public int? PreviousVersionId { get; set; }
        public Document? PreviousVersion { get; set; }

        // Uploaded by
        public int UploadedById { get; set; }
        public AppUser? UploadedBy { get; set; }

        // Associated entities
        public int? RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }

        public int? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }

        // Navigation
        public ICollection<Document> NextVersions { get; set; } = new List<Document>();
    }
}
