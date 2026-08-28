using Zentic.Application.DTOs;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<PaginatedResult<ExpenseRequisitionDto>> GetPagedAsync(ExpenseFilterDto filter, int userId, string role, CancellationToken cancellationToken = default);
        Task<ExpenseRequisitionDto?> GetByIdAsync(int id, int userId, string role, CancellationToken cancellationToken = default);
        Task<ExpenseRequisitionDto> CreateAsync(CreateExpenseDto dto, int userId, CancellationToken cancellationToken = default);
        Task SubmitAsync(int id, int userId, CancellationToken cancellationToken = default);
        Task ApproveAsync(int id, int approverId, string? notes, CancellationToken cancellationToken = default);
        Task RejectAsync(int id, int approverId, string reason, CancellationToken cancellationToken = default);
        Task CancelAsync(int id, int userId, CancellationToken cancellationToken = default);
        Task MarkAsPaidAsync(int id, int userId, CancellationToken cancellationToken = default);
        Task<List<QuotationDto>> GetQuotationsAsync(int requisitionId, CancellationToken cancellationToken = default);
        Task AddQuotationAsync(AddQuotationDto dto, int userId, CancellationToken cancellationToken = default);
        Task RemoveQuotationAsync(int quotationId, int userId, CancellationToken cancellationToken = default);
        Task SelectQuotationAsync(int requisitionId, int quotationId, string? reason, int userId, CancellationToken cancellationToken = default);
    }
}
