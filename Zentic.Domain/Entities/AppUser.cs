using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class AppUser : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string? Position { get; set; }
        public UserRole Role { get; set; } = UserRole.Staff;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }

        // FK
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Navigation
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
        public ICollection<ExpenseRequisition> ApprovedRequisitions { get; set; } = new List<ExpenseRequisition>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
