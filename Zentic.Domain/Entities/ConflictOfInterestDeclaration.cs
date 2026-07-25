namespace Zentic.Domain.Entities
{
    /// <summary>
    /// Conflict of interest declaration — requester wajib isi saat pilih vendor
    /// </summary>
    public class ConflictOfInterestDeclaration : BaseEntity
    {
        public int RequisitionId { get; set; }
        public ExpenseRequisition? Requisition { get; set; }

        public int DeclarantId { get; set; }
        public AppUser? Declarant { get; set; }

        public bool HasConflict { get; set; } = false;
        public string? ConflictDescription { get; set; }

        /// <summary>
        /// Digital signature (hash dari user ID + timestamp + requisition ID)
        /// untuk bukti deklarasi tidak bisa diubah
        /// </summary>
        public string SignatureHash { get; set; } = string.Empty;
        public DateTime DeclaredAt { get; set; } = DateTime.UtcNow;
    }
}
