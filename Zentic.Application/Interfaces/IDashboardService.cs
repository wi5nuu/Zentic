using Zentic.Application.DTOs;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(int userId, string role, CancellationToken cancellationToken = default);
    }
}
