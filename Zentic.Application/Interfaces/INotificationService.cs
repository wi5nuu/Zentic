using Zentic.Application.DTOs;

namespace Zentic.Application.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int createdById);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 50);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteAsync(int notificationId, int userId);
        Task SendNotificationAsync(int notificationId);
        Task NotifyRequisitionSubmittedAsync(int requisitionId, int managerId);
        Task NotifyRequisitionApprovedAsync(int requisitionId);
        Task NotifyRequisitionRejectedAsync(int requisitionId, string reason);
        Task NotifyPaymentCompletedAsync(int paymentId);
        Task NotifyBudgetThresholdExceededAsync(int budgetId, decimal utilizationPercentage);
    }
}