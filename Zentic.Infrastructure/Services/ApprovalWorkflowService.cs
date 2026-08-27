using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly AppDbContext _db;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public ApprovalWorkflowService(AppDbContext db, IAuditService auditService, INotificationService notificationService, IUnitOfWork unitOfWork)
        {
            _db = db;
            _auditService = auditService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApprovalWorkflowDto> CreateWorkflowAsync(CreateApprovalWorkflowDto dto, int createdById)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var workflow = new ApprovalWorkflow
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    DepartmentId = dto.DepartmentId,
                    Category = dto.Category,
                    MinAmount = dto.MinAmount,
                    MaxAmount = dto.MaxAmount,
                    Priority = dto.Priority,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.ApprovalWorkflows.Add(workflow);
                await _unitOfWork.SaveChangesAsync();

                // Add steps
                for (int i = 0; i < dto.Steps.Count; i++)
                {
                    var stepDto = dto.Steps[i];
                    var step = new ApprovalStep
                    {
                        WorkflowId = workflow.Id,
                        StepOrder = i + 1,
                        Name = stepDto.Name,
                        Description = stepDto.Description,
                        ApproverType = stepDto.ApproverType,
                        SpecificUserId = stepDto.SpecificUserId,
                        RequiredRole = stepDto.RequiredRole,
                        DepartmentId = stepDto.DepartmentId,
                        AllowDelegation = stepDto.AllowDelegation,
                        RequireAllApprovers = stepDto.RequireAllApprovers,
                        EscalationRuleId = stepDto.EscalationRuleId,
                        IsParallel = stepDto.IsParallel,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.ApprovalSteps.Add(step);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                await _auditService.LogAsync(AuditEventType.Create, nameof(ApprovalWorkflow), workflow.Id.ToString(),
                    createdById, null, null, System.Text.Json.JsonSerializer.Serialize(new { workflow.Id, workflow.Name }), "Approval workflow created", null);

                return (await GetWorkflowByIdAsync(workflow.Id))!;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<ApprovalWorkflowDto?> GetWorkflowByIdAsync(int id)
        {
            var workflow = await _db.ApprovalWorkflows
                .Include(w => w.Department)
                .Include(w => w.Steps)
                    .ThenInclude(s => s.SpecificUser)
                .Include(w => w.Steps)
                    .ThenInclude(s => s.Department)
                .Include(w => w.Steps)
                    .ThenInclude(s => s.EscalationRule)
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (workflow == null) return null;

            return MapToDto(workflow);
        }

        public async Task<List<ApprovalWorkflowDto>> GetActiveWorkflowsAsync(int? departmentId = null, ExpenseCategory? category = null, decimal? amount = null)
        {
            var query = _db.ApprovalWorkflows
                .Include(w => w.Department)
                .Include(w => w.Steps)
                .Where(w => w.IsActive && !w.IsDeleted);

            if (departmentId.HasValue)
                query = query.Where(w => w.DepartmentId == departmentId || w.DepartmentId == null);

            if (category.HasValue)
                query = query.Where(w => w.Category == category || w.Category == null);

            if (amount.HasValue)
                query = query.Where(w => (w.MinAmount == null || w.MinAmount <= amount) && (w.MaxAmount == null || w.MaxAmount >= amount));

            var workflows = await query
                .OrderBy(w => w.Priority)
                .ToListAsync();

            return workflows.Select(MapToDto).ToList();
        }

        public async Task<ApprovalWorkflowDto> UpdateWorkflowAsync(int id, CreateApprovalWorkflowDto dto, int updatedById)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var workflow = await _db.ApprovalWorkflows
                    .Include(w => w.Steps)
                    .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

                if (workflow == null) throw new InvalidOperationException("Workflow not found.");

                workflow.Name = dto.Name;
                workflow.Description = dto.Description;
                workflow.DepartmentId = dto.DepartmentId;
                workflow.Category = dto.Category;
                workflow.MinAmount = dto.MinAmount;
                workflow.MaxAmount = dto.MaxAmount;
                workflow.Priority = dto.Priority;
                workflow.UpdatedAt = DateTime.UtcNow;

                // Remove existing steps
                _db.ApprovalSteps.RemoveRange(workflow.Steps);

                // Add new steps
                for (int i = 0; i < dto.Steps.Count; i++)
                {
                    var stepDto = dto.Steps[i];
                    var step = new ApprovalStep
                    {
                        WorkflowId = workflow.Id,
                        StepOrder = i + 1,
                        Name = stepDto.Name,
                        Description = stepDto.Description,
                        ApproverType = stepDto.ApproverType,
                        SpecificUserId = stepDto.SpecificUserId,
                        RequiredRole = stepDto.RequiredRole,
                        DepartmentId = stepDto.DepartmentId,
                        AllowDelegation = stepDto.AllowDelegation,
                        RequireAllApprovers = stepDto.RequireAllApprovers,
                        EscalationRuleId = stepDto.EscalationRuleId,
                        IsParallel = stepDto.IsParallel,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.ApprovalSteps.Add(step);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                await _auditService.LogAsync(AuditEventType.Update, nameof(ApprovalWorkflow), workflow.Id.ToString(),
                    updatedById, null, null, System.Text.Json.JsonSerializer.Serialize(new { workflow.Id, workflow.Name }), "Approval workflow updated", null);

                return (await GetWorkflowByIdAsync(workflow.Id))!;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteWorkflowAsync(int id, int deletedById)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var workflow = await _db.ApprovalWorkflows.FindAsync(id);
                if (workflow == null) return false;

                workflow.IsDeleted = true;
                workflow.DeletedAt = DateTime.UtcNow;
                workflow.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                await _auditService.LogAsync(AuditEventType.Delete, nameof(ApprovalWorkflow), id.ToString(),
                    deletedById, null, null, null, "Approval workflow deleted", null);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<EscalationRuleDto> CreateEscalationRuleAsync(CreateEscalationRuleDto dto, int createdById)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var rule = new EscalationRule
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    EscalationAfterHours = dto.EscalationAfterHours,
                    Trigger = dto.Trigger,
                    EscalateToUserId = dto.EscalateToUserId,
                    EscalateToRole = dto.EscalateToRole,
                    DepartmentId = dto.DepartmentId,
                    MinAmount = dto.MinAmount,
                    MaxAmount = dto.MaxAmount,
                    IsActive = true,
                    NotifyOriginalApprover = dto.NotifyOriginalApprover,
                    NotifyEscalationTarget = dto.NotifyEscalationTarget,
                    CustomMessage = dto.CustomMessage,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.EscalationRules.Add(rule);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                await _auditService.LogAsync(AuditEventType.Create, nameof(EscalationRule), rule.Id.ToString(),
                    createdById, null, null, System.Text.Json.JsonSerializer.Serialize(new { rule.Id, rule.Name }), "Escalation rule created", null);

                return MapToDto(rule);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<ApprovalDelegationDto> CreateDelegationAsync(CreateApprovalDelegationDto dto, int delegatorId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate delegator exists
                var delegator = await _db.Users.FindAsync(delegatorId);
                if (delegator == null) throw new InvalidOperationException("Delegator not found.");

                // Validate delegate exists
                var delegateUser = await _db.Users.FindAsync(dto.DelegateId);
                if (delegateUser == null) throw new InvalidOperationException("Delegate user not found.");

                // Check for overlapping delegation for this delegator
                var existing = await _db.ApprovalDelegations
                    .FirstOrDefaultAsync(d => d.DelegatorId == delegatorId
                        && d.IsActive
                        && d.StartDate <= dto.EndDate
                        && d.EndDate >= dto.StartDate
                        && !d.IsDeleted);

                if (existing != null)
                    throw new InvalidOperationException("An active delegation already exists for this period.");

                var delegation = new ApprovalDelegation
                {
                    DelegatorId = delegatorId,
                    DelegateId = dto.DelegateId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Reason = dto.Reason,
                    DepartmentId = dto.DepartmentId,
                    MaxAmount = dto.MaxAmount,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.ApprovalDelegations.Add(delegation);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                await _auditService.LogAsync(AuditEventType.Create, nameof(ApprovalDelegation), delegation.Id.ToString(),
                    delegatorId, null, null, System.Text.Json.JsonSerializer.Serialize(new { delegation.Id, delegation.DelegatorId, delegation.DelegateId }), "Approval delegation created", null);

                // Notify delegate
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    Title = "Approval Delegation Assigned",
                    Message = $"You have been assigned as an approval delegate by {delegator.FullName} from {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}.",
                    Type = NotificationType.InApp,
                    Priority = NotificationPriority.Normal,
                    RecipientId = dto.DelegateId,
                    EntityType = nameof(ApprovalDelegation),
                    EntityId = delegation.Id.ToString(),
                    ActionUrl = $"/Approval/Delegations/{delegation.Id}"
                }, 0);

                return MapToDto(delegation);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RevokeDelegationAsync(int delegationId, int delegatorId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var delegation = await _db.ApprovalDelegations
                    .FirstOrDefaultAsync(d => d.Id == delegationId && d.DelegatorId == delegatorId && !d.IsDeleted);

                if (delegation == null) return false;

                delegation.IsActive = false;
                delegation.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                await _auditService.LogAsync(AuditEventType.Delete, nameof(ApprovalDelegation), delegationId.ToString(),
                    delegatorId, null, null, null, "Approval delegation revoked", null);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ApprovalDelegationDto>> GetActiveDelegationsAsync(int userId)
        {
            var now = DateTime.UtcNow;
            var delegations = await _db.ApprovalDelegations
                .Include(d => d.Delegator)
                .Include(d => d.Delegate)
                .Include(d => d.Department)
                .Where(d => d.DelegateId == userId
                    && d.IsActive
                    && d.StartDate <= now
                    && d.EndDate >= now
                    && !d.IsDeleted)
                .ToListAsync();

            return delegations.Select(MapToDto).ToList();
        }

        public async Task<List<ApprovalDelegationDto>> GetDelegationsByDelegatorAsync(int delegatorId)
        {
            var now = DateTime.UtcNow;
            var delegations = await _db.ApprovalDelegations
                .Include(d => d.Delegator)
                .Include(d => d.Delegate)
                .Include(d => d.Department)
                .Where(d => d.DelegatorId == delegatorId
                    && d.IsActive
                    && d.EndDate >= now
                    && !d.IsDeleted)
                .OrderByDescending(d => d.StartDate)
                .ToListAsync();

            return delegations.Select(MapToDto).ToList();
        }

        public async Task<ApprovalResultDto> ProcessApprovalAsync(int requisitionId, int approverId, ApprovalAction action, string? notes)
        {
            var requisition = await _db.ExpenseRequisitions
                .Include(r => r.Requester)
                .Include(r => r.Department)
                .FirstOrDefaultAsync(r => r.Id == requisitionId && !r.IsDeleted);

            if (requisition == null)
                return new ApprovalResultDto { Success = false, Message = "Requisition not found." };

            // Check if user is authorized to approve
            var canApprove = await CanUserApproveAtStepAsync(requisition, approverId);
            if (!canApprove)
                return new ApprovalResultDto { Success = false, Message = "You are not authorized to approve this requisition at this stage." };

            var oldStatus = requisition.Status;

            if (action == ApprovalAction.Approve)
            {
                requisition.Status = RequisitionStatus.Approved;
                requisition.ApproverId = approverId;
                requisition.ApprovedAt = DateTime.UtcNow;
                requisition.ApprovalNotes = notes;
            }
            else
            {
                requisition.Status = RequisitionStatus.Rejected;
                requisition.ApproverId = approverId;
                requisition.RejectedAt = DateTime.UtcNow;
                requisition.RejectionReason = notes;
            }

            requisition.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Record approval history
            var history = new RequisitionApprovalHistory
            {
                RequisitionId = requisitionId,
                ActorId = approverId,
                Action = action,
                FromStatus = oldStatus,
                ToStatus = requisition.Status,
                Notes = notes,
                Timestamp = DateTime.UtcNow
            };
            _db.RequisitionApprovalHistories.Add(history);
            await _db.SaveChangesAsync();

            // Notify requester
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                Title = action == ApprovalAction.Approve ? "Requisition Approved" : "Requisition Rejected",
                Message = $"Your requisition {requisition.RequisitionNumber} has been {(action == ApprovalAction.Approve ? "approved" : "rejected")}.{(string.IsNullOrEmpty(notes) ? "" : $" Reason: {notes}")}",
                Type = NotificationType.InApp,
                Priority = NotificationPriority.Normal,
                RecipientId = requisition.RequesterId,
                EntityType = nameof(ExpenseRequisition),
                EntityId = requisitionId.ToString(),
                ActionUrl = $"/Expense/Details/{requisitionId}"
            }, 0);

            await _auditService.LogAsync(
                action == ApprovalAction.Approve ? AuditEventType.Approve : AuditEventType.Reject,
                nameof(ExpenseRequisition), requisitionId.ToString(),
                approverId, null, oldStatus.ToString(), requisition.Status.ToString(), notes, requisitionId);

            return new ApprovalResultDto { Success = true, Message = $"Requisition {(action == ApprovalAction.Approve ? "approved" : "rejected")} successfully." };
        }

        private async Task<bool> CanUserApproveAtStepAsync(ExpenseRequisition requisition, int userId)
        {
            // Find applicable workflow - query directly to get entities with steps
            var workflow = await _db.ApprovalWorkflows
                .Include(w => w.Steps)
                    .ThenInclude(s => s.SpecificUser)
                .Include(w => w.Steps)
                    .ThenInclude(s => s.Department)
                .Include(w => w.Steps)
                    .ThenInclude(s => s.EscalationRule)
                .Where(w => w.IsActive && !w.IsDeleted)
                .Where(w => w.DepartmentId == requisition.DepartmentId || w.DepartmentId == null)
                .Where(w => w.Category == requisition.Category || w.Category == null)
                .Where(w => w.MinAmount == null || w.MinAmount <= requisition.Amount)
                .Where(w => w.MaxAmount == null || w.MaxAmount >= requisition.Amount)
                .OrderBy(w => w.Priority)
                .FirstOrDefaultAsync();

            if (workflow == null)
            {
                // Fallback to role-based authorization
                var user = await _db.Users.FindAsync(userId);
                return user?.Role == UserRole.Admin || user?.Role == UserRole.Finance || user?.Role == UserRole.Manager;
            }

            // Get current step based on requisition status
            var currentStep = workflow.Steps
                .OrderBy(s => s.StepOrder)
                .FirstOrDefault(s => IsStepApplicable(s, requisition.Status));

            if (currentStep == null) return false;

            // Check if user is an approver for this step
            return await IsUserApproverForStepAsync(currentStep, userId, requisition);
        }

        private bool IsStepApplicable(ApprovalStep step, RequisitionStatus status)
        {
            // Map status to step order
            // This is simplified - in reality you'd have more complex logic
            return step.StepOrder switch
            {
                1 => status == RequisitionStatus.Submitted || status == RequisitionStatus.UnderReview,
                2 => status == RequisitionStatus.PendingFinanceApproval,
                _ => false
            };
        }

        private async Task<bool> IsUserApproverForStepAsync(ApprovalStep step, int userId, ExpenseRequisition requisition)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            // Check active delegations
            var now = DateTime.UtcNow;
            var delegation = await _db.ApprovalDelegations
                .FirstOrDefaultAsync(d => d.DelegateId == userId
                    && d.IsActive
                    && d.StartDate <= now
                    && d.EndDate >= now
                    && (d.DepartmentId == null || d.DepartmentId == requisition.DepartmentId)
                    && (d.MaxAmount == null || d.MaxAmount >= requisition.Amount)
                    && !d.IsDeleted);

            if (delegation != null)
            {
                // Check if delegator is an approver for this step
                return await IsUserApproverForStepAsync(step, delegation.DelegatorId, requisition);
            }

            // Check based on approver type
            return step.ApproverType switch
            {
                ApproverType.SpecificUser => step.SpecificUserId == userId,
                ApproverType.Role => user.Role == step.RequiredRole,
                ApproverType.Manager => requisition.Requester?.ManagerId == userId,
                ApproverType.DepartmentHead => requisition.Department?.ManagerId == userId,
                ApproverType.Finance => user.Role == UserRole.Finance,
                ApproverType.Admin => user.Role == UserRole.Admin,
                _ => false
            };
        }

        private static ApprovalWorkflowDto MapToDto(ApprovalWorkflow workflow)
        {
            return new ApprovalWorkflowDto
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                DepartmentId = workflow.DepartmentId,
                DepartmentName = workflow.Department?.Name,
                Category = workflow.Category?.ToString(),
                MinAmount = workflow.MinAmount,
                MaxAmount = workflow.MaxAmount,
                Priority = workflow.Priority,
                IsActive = workflow.IsActive,
                Steps = workflow.Steps.OrderBy(s => s.StepOrder).Select(MapToDto).ToList()
            };
        }

        private static ApprovalStepDto MapToDto(ApprovalStep step)
        {
            return new ApprovalStepDto
            {
                Id = step.Id,
                StepOrder = step.StepOrder,
                Name = step.Name,
                Description = step.Description,
                ApproverType = step.ApproverType.ToString(),
                SpecificUserId = step.SpecificUserId,
                SpecificUserName = step.SpecificUser?.FullName,
                RequiredRole = step.RequiredRole?.ToString(),
                DepartmentId = step.DepartmentId,
                DepartmentName = step.Department?.Name,
                AllowDelegation = step.AllowDelegation,
                RequireAllApprovers = step.RequireAllApprovers,
                EscalationRuleId = step.EscalationRuleId,
                EscalationRuleName = step.EscalationRule?.Name,
                IsParallel = step.IsParallel
            };
        }

        private static EscalationRuleDto MapToDto(EscalationRule rule)
        {
            return new EscalationRuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                EscalationAfterHours = rule.EscalationAfterHours,
                Trigger = rule.Trigger.ToString(),
                EscalateToUserId = rule.EscalateToUserId,
                EscalateToUserName = rule.EscalateToUser?.FullName,
                EscalateToRole = rule.EscalateToRole?.ToString(),
                DepartmentId = rule.DepartmentId,
                DepartmentName = rule.Department?.Name,
                MinAmount = rule.MinAmount,
                MaxAmount = rule.MaxAmount,
                IsActive = rule.IsActive,
                NotifyOriginalApprover = rule.NotifyOriginalApprover,
                NotifyEscalationTarget = rule.NotifyEscalationTarget,
                CustomMessage = rule.CustomMessage
            };
        }

        private static ApprovalDelegationDto MapToDto(ApprovalDelegation delegation)
        {
            return new ApprovalDelegationDto
            {
                Id = delegation.Id,
                DelegatorId = delegation.DelegatorId,
                DelegatorName = delegation.Delegator?.FullName,
                DelegateId = delegation.DelegateId,
                DelegateName = delegation.Delegate?.FullName,
                StartDate = delegation.StartDate,
                EndDate = delegation.EndDate,
                IsActive = delegation.IsActive,
                Reason = delegation.Reason,
                DepartmentId = delegation.DepartmentId,
                DepartmentName = delegation.Department?.Name,
                MaxAmount = delegation.MaxAmount
            };
        }

        public async Task<List<EscalationRuleDto>> GetActiveEscalationRulesAsync()
        {
            var rules = await _db.EscalationRules
                .Include(e => e.EscalateToUser)
                .Include(e => e.Department)
                .Where(e => e.IsActive && !e.IsDeleted)
                .ToListAsync();

            return rules.Select(MapToDto).ToList();
        }
    }
}