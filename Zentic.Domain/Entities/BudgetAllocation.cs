using System.ComponentModel.DataAnnotations;
using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class BudgetAllocation : BaseEntity
    {
        public string BudgetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Budget details
        private decimal _allocatedAmount;
        public decimal AllocatedAmount
        {
            get => _allocatedAmount;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "AllocatedAmount cannot be negative.");
                _allocatedAmount = value;
            }
        }

        private decimal _spentAmount = 0;
        public decimal SpentAmount
        {
            get => _spentAmount;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "SpentAmount cannot be negative.");
                _spentAmount = value;
            }
        }

        private decimal _committedAmount = 0;
        public decimal CommittedAmount
        {
            get => _committedAmount;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "CommittedAmount cannot be negative.");
                _committedAmount = value;
            }
        }

        public decimal AvailableAmount => AllocatedAmount - SpentAmount - CommittedAmount;
        public Currency Currency { get; set; } = Currency.IDR;

        // Period
        public BudgetPeriodType PeriodType { get; set; }
        public int FiscalYear { get; set; }

        private int? _quarter;
        public int? Quarter
        {
            get => _quarter;
            set
            {
                if (value.HasValue && (value < 1 || value > 4))
                    throw new ArgumentOutOfRangeException(nameof(value), "Quarter must be between 1 and 4.");
                _quarter = value;
            }
        }

        private int? _month;
        public int? Month
        {
            get => _month;
            set
            {
                if (value.HasValue && (value < 1 || value > 12))
                    throw new ArgumentOutOfRangeException(nameof(value), "Month must be between 1 and 12.");
                _month = value;
            }
        }

        private DateTime _periodStartDate;
        public DateTime PeriodStartDate
        {
            get => _periodStartDate;
            set
            {
                _periodStartDate = value;
                if (_periodEndDate != default && value > _periodEndDate)
                    throw new ArgumentOutOfRangeException(nameof(value), "PeriodStartDate cannot be after PeriodEndDate.");
            }
        }

        private DateTime _periodEndDate;
        public DateTime PeriodEndDate
        {
            get => _periodEndDate;
            set
            {
                _periodEndDate = value;
                if (_periodStartDate != default && value < _periodStartDate)
                    throw new ArgumentOutOfRangeException(nameof(value), "PeriodEndDate cannot be before PeriodStartDate.");
            }
        }

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
        private decimal _warningThresholdPercentage = 80;
        public decimal WarningThresholdPercentage
        {
            get => _warningThresholdPercentage;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "WarningThresholdPercentage must be between 0 and 100.");
                _warningThresholdPercentage = value;
            }
        }
        public bool IsOverBudget => SpentAmount + CommittedAmount > AllocatedAmount;
        public decimal UtilizationPercentage => AllocatedAmount > 0 ? ((SpentAmount + CommittedAmount) / AllocatedAmount) * 100 : 0;

        // Navigation
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
    }
}
