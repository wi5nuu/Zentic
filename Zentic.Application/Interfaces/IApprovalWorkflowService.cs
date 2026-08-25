using Zentic.Application.DTOs;
using Zentic.Domain.Enums;

namespace Zentic.Application.Interfaces
{
    public interface IApprovalWorkflowService
    {
        Task<ApprovalWorkflowDto> CreateWorkflowAsync(CreateApprovalWorkflowDto dto, int createdById);
        Task<ApprovalWorkflowDto?> GetWorkflowByIdAsync(int id);
        Task<List<ApprovalWorkflowDto>> GetActiveWorkflowsAsync(int? departmentId = null, ExpenseCategory? category = null, decimal? amount = null);
        Task<ApprovalWorkflowDto> UpdateWorkflowAsync(int id, CreateApprovalWorkflowDto dto, int updatedById);
        Task<bool> DeleteWorkflowAsync(int id, int deletedById);

        Task<EscalationRuleDto> CreateEscalationRuleAsync(CreateEscalationRuleDto dto, int createdById);
        Task<List<EscalationRuleDto>> GetActiveEscalationRulesAsync();

        Task<ApprovalDelegationDto> CreateDelegationAsync(CreateApprovalDelegationDto dto, int delegatorId);
        Task<bool> RevokeDelegationAsync(int delegationId, int delegatorId);
        Task<List<ApprovalDelegationDto>> GetActiveDelegationsAsync(int userId);
        Task<List<ApprovalDelegationDto>> GetDelegationsByDelegatorAsync(int delegatorId);

        Task<ApprovalResultDto> ProcessApprovalAsync(int requisitionId, int approverId, ApprovalAction action, string? notes);
    }
}