using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    /// <summary>
    /// Approval delegation - allows users to delegate their approval authority
    /// </summary>
    public class ApprovalDelegation : BaseEntity
    {
        public int DelegatorId { get; set; }
        public AppUser? Delegator { get; set; }

        public int DelegateId { get; set; }
        public AppUser? Delegate { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Reason { get; set; }

        // Scope of delegation
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public decimal? MaxAmount { get; set; } // Null = unlimited
    }

    /// <summary>
    /// Escalation rules for approval workflows
    /// </summary>
    public class EscalationRule : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Trigger conditions
        public int EscalationAfterHours { get; set; } = 24; // Hours before escalation
        public EscalationTrigger Trigger { get; set; } = EscalationTrigger.NoAction;

        // Escalation target
        public int? EscalateToUserId { get; set; }
        public AppUser? EscalateToUser { get; set; }

        public int? EscalateToRoleId { get; set; }
        public UserRole? EscalateToRole { get; set; }

        // Scope
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public bool IsActive { get; set; } = true;

        // Notification settings
        public bool NotifyOriginalApprover { get; set; } = true;
        public bool NotifyEscalationTarget { get; set; } = true;
        public string? CustomMessage { get; set; }
    }

    /// <summary>
    /// Approval workflow definition
    /// </summary>
    public class ApprovalWorkflow : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Workflow applies to
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public ExpenseCategory? Category { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        // Priority (lower number = higher priority)
        public int Priority { get; set; } = 10;

        // Navigation
        public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    }

    /// <summary>
    /// Step in an approval workflow
    /// </summary>
    public class ApprovalStep : BaseEntity
    {
        public int WorkflowId { get; set; }
        public ApprovalWorkflow? Workflow { get; set; }

        public int StepOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Approver determination
        public ApproverType ApproverType { get; set; } = ApproverType.SpecificUser;
        public int? SpecificUserId { get; set; }
        public AppUser? SpecificUser { get; set; }

        public UserRole? RequiredRole { get; set; }
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Step settings
        public bool AllowDelegation { get; set; } = true;
        public bool RequireAllApprovers { get; set; } = false; // If multiple approvers match
        public int? EscalationRuleId { get; set; }
        public EscalationRule? EscalationRule { get; set; }

        // Parallel/Sequential
        public bool IsParallel { get; set; } = false;
    }
}