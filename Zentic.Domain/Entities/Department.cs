namespace Zentic.Domain.Entities
{
    public class Department : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CostCenter { get; set; }
        public bool IsActive { get; set; } = true;

        // Department hierarchy
        public int? ParentDepartmentId { get; set; }
        public Department? ParentDepartment { get; set; }

        // Department head
        public int? ManagerId { get; set; }
        public AppUser? Manager { get; set; }

        // Navigation
        public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
        public ICollection<BudgetAllocation> BudgetAllocations { get; set; } = new List<BudgetAllocation>();
    }
}
