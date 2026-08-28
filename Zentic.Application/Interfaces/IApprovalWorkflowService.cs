using Zentic.Application.DTOs;
using Zentic.Domain.Enums;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface IApprovalWorkflowService
    {
        Task<ApprovalWorkflowDto> CreateWorkflowAsync(CreateApprovalWorkflowDto dto, int createdById, CancellationToken cancellationToken = default);
        Task<ApprovalWorkflowDto?> GetWorkflowByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<ApprovalWorkflowDto>> GetActiveWorkflowsAsync(int? departmentId = null, ExpenseCategory? category = null, decimal? amount = null, CancellationToken cancellationToken = default);
        Task<ApprovalWorkflowDto> UpdateWorkflowAsync(int id, CreateApprovalWorkflowDto dto, int updatedById, CancellationToken cancellationToken = default);
        Task<bool> DeleteWorkflowAsync(int id, int deletedById, CancellationToken cancellationToken = default);

        Task<EscalationRuleDto> CreateEscalationRuleAsync(CreateEscalationRuleDto dto, int createdById, CancellationToken cancellationToken = default);
        Task<List<EscalationRuleDto>> GetActiveEscalationRulesAsync(CancellationToken cancellationToken = default);

        Task<ApprovalDelegationDto> CreateDelegationAsync(CreateApprovalDelegationDto dto, int delegatorId, CancellationToken cancellationToken = default);
        Task<bool> RevokeDelegationAsync(int delegationId, int delegatorId, CancellationToken cancellationToken = default);
        Task<List<ApprovalDelegationDto>> GetActiveDelegationsAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<ApprovalDelegationDto>> GetDelegationsByDelegatorAsync(int delegatorId, CancellationToken cancellationToken = default);

        Task<ApprovalResultDto> ProcessApprovalAsync(int requisitionId, int approverId, ApprovalAction action, string? notes, CancellationToken cancellationToken = default);
    }
}