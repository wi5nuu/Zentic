using Zentic.Application.DTOs;

namespace Zentic.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<PaginatedResult<ExpenseRequisitionDto>> GetPagedAsync(ExpenseFilterDto filter, int userId, string role);
        Task<ExpenseRequisitionDto?> GetByIdAsync(int id, int userId, string role);
        Task<ExpenseRequisitionDto> CreateAsync(CreateExpenseDto dto, int userId);
        Task SubmitAsync(int id, int userId);
        Task ApproveAsync(int id, int approverId, string? notes);
        Task RejectAsync(int id, int approverId, string reason);
        Task CancelAsync(int id, int userId);
    }
}
