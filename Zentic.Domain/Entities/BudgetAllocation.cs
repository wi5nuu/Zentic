using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class BudgetAllocation : BaseEntity
    {
        public string BudgetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Budget details
        public decimal AllocatedAmount { get; set; }
        public decimal SpentAmount { get; set; } = 0;
        public decimal CommittedAmount { get; set; } = 0; // Pending approvals
        public decimal AvailableAmount => AllocatedAmount - SpentAmount - CommittedAmount;
        public Currency Currency { get; set; } = Currency.IDR;

        // Period
        public BudgetPeriodType PeriodType { get; set; }
        public int FiscalYear { get; set; }
        public int? Quarter { get; set; } // 1-4 for quarterly
        public int? Month { get; set; } // 1-12 for monthly
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }

        // Category & Department
        public ExpenseCategory? Category { get; set; }
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Status & Approval
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public int? ApprovedById { get; set; }
        public AppUser? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Alerts
        public decimal WarningThresholdPercentage { get; set; } = 80; // Alert at 80%
        public bool IsOverBudget => SpentAmount + CommittedAmount > AllocatedAmount;
        public decimal UtilizationPercentage => AllocatedAmount > 0 ? ((SpentAmount + CommittedAmount) / AllocatedAmount) * 100 : 0;

        // Navigation
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
    }
}
