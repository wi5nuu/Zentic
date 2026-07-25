namespace Zentic.Domain.Entities
{
    public class Department : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
    }
}
