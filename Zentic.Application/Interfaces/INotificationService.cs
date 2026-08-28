using Zentic.Application.DTOs;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int createdById, CancellationToken cancellationToken = default);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 50, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task SendNotificationAsync(int notificationId, CancellationToken cancellationToken = default);
        Task NotifyRequisitionSubmittedAsync(int requisitionId, int managerId, CancellationToken cancellationToken = default);
        Task NotifyRequisitionApprovedAsync(int requisitionId, CancellationToken cancellationToken = default);
        Task NotifyRequisitionRejectedAsync(int requisitionId, string reason, CancellationToken cancellationToken = default);
        Task NotifyPaymentCompletedAsync(int paymentId, CancellationToken cancellationToken = default);
        Task NotifyBudgetThresholdExceededAsync(int budgetId, decimal utilizationPercentage, CancellationToken cancellationToken = default);
    }
}