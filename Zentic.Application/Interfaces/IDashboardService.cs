using Zentic.Application.DTOs;

namespace Zentic.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(int userId, string role);
    }
}
