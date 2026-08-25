using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<Notification> _notificationRepo;

        public NotificationService(AppDbContext db, IRepository<Notification> notificationRepo)
        {
            _db = db;
            _notificationRepo = notificationRepo;
        }

        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int createdById)
        {
            var notification = new Notification
            {
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                Priority = dto.Priority,
                RecipientId = dto.RecipientId,
                EntityType = dto.EntityType,
                EntityId = dto.EntityId,
                ActionUrl = dto.ActionUrl,
                TemplateName = dto.TemplateName,
                IsRead = false,
                IsSent = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepo.AddAsync(notification);
            await _db.SaveChangesAsync();

            // Try to send notification immediately
            await SendNotificationAsync(notification.Id);

            return MapToDto(notification);
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 50)
        {
            var query = _notificationRepo.Query()
                .Where(n => n.RecipientId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return notifications.Select(MapToDto).ToList();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notificationRepo.Query()
                .CountAsync(n => n.RecipientId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);
            
            if (notification == null || notification.RecipientId != userId)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            
            await _notificationRepo.UpdateAsync(notification);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var notifications = await _notificationRepo.Query()
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);
            
            if (notification == null || notification.RecipientId != userId)
                return false;

            await _notificationRepo.SoftDeleteAsync(notificationId);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task SendNotificationAsync(int notificationId)
        {
            var notification = await _db.Notifications
                .Include(n => n.Recipient)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null || notification.IsSent)
                return;

            try
            {
                // TODO: Implement actual email/SMS sending logic here
                // For now, just mark as sent
                
                if (notification.Type == NotificationType.Email && !string.IsNullOrEmpty(notification.Recipient?.Email))
                {
                    notification.EmailTo = notification.Recipient.Email;
                    notification.EmailSubject = notification.Title;
                    // await _emailService.SendAsync(notification.EmailTo, notification.EmailSubject, notification.Message);
                }
                else if (notification.Type == NotificationType.SMS && !string.IsNullOrEmpty(notification.Recipient?.PhoneNumber))
                {
                    notification.SmsTo = notification.Recipient.PhoneNumber;
                    // await _smsService.SendAsync(notification.SmsTo, notification.Message);
                }

                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                notification.RetryCount++;
                notification.ErrorMessage = ex.Message;
            }

            await _db.SaveChangesAsync();
        }

        public async Task NotifyRequisitionSubmittedAsync(int requisitionId, int managerId)
        {
            var requisition = await _db.ExpenseRequisitions
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requisitionId);

            if (requisition == null) return;

            await CreateAsync(new CreateNotificationDto
            {
                Title = "New Requisition Awaiting Your Approval",
                Message = $"Requisition {requisition.RequisitionNumber} from {requisition.Requester?.FullName} requires your approval. Amount: {requisition.Amount:N2} {requisition.Currency}",
                Type = NotificationType.InApp,
                Priority = NotificationPriority.High,
                RecipientId = managerId,
                EntityType = nameof(ExpenseRequisition),
                EntityId = requisitionId.ToString(),
                ActionUrl = $"/Expense/Details/{requisitionId}"
            }, 0);
        }

        public async Task NotifyRequisitionApprovedAsync(int requisitionId)
        {
            var requisition = await _db.ExpenseRequisitions
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requisitionId);

            if (requisition == null) return;

            await CreateAsync(new CreateNotificationDto
            {
                Title = "Requisition Approved",
                Message = $"Your requisition {requisition.RequisitionNumber} has been approved.",
                Type = NotificationType.InApp,
                Priority = NotificationPriority.Normal,
                RecipientId = requisition.RequesterId,
                EntityType = nameof(ExpenseRequisition),
                EntityId = requisitionId.ToString(),
                ActionUrl = $"/Expense/Details/{requisitionId}"
            }, 0);
        }

        public async Task NotifyRequisitionRejectedAsync(int requisitionId, string reason)
        {
            var requisition = await _db.ExpenseRequisitions
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requisitionId);

            if (requisition == null) return;

            await CreateAsync(new CreateNotificationDto
            {
                Title = "Requisition Rejected",
                Message = $"Your requisition {requisition.RequisitionNumber} has been rejected. Reason: {reason}",
                Type = NotificationType.InApp,
                Priority = NotificationPriority.High,
                RecipientId = requisition.RequesterId,
                EntityType = nameof(ExpenseRequisition),
                EntityId = requisitionId.ToString(),
                ActionUrl = $"/Expense/Details/{requisitionId}"
            }, 0);
        }

        public async Task NotifyPaymentCompletedAsync(int paymentId)
        {
            var payment = await _db.Payments
                .Include(p => p.Requisition)
                    .ThenInclude(r => r!.Requester)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment?.Requisition == null) return;

            await CreateAsync(new CreateNotificationDto
            {
                Title = "Payment Completed",
                Message = $"Payment for requisition {payment.Requisition.RequisitionNumber} has been completed. Amount: {payment.Amount:N2} {payment.Currency}",
                Type = NotificationType.InApp,
                Priority = NotificationPriority.Normal,
                RecipientId = payment.Requisition.RequesterId,
                EntityType = nameof(Payment),
                EntityId = paymentId.ToString(),
                ActionUrl = $"/Expense/Details/{payment.RequisitionId}"
            }, 0);
        }

        public async Task NotifyBudgetThresholdExceededAsync(int budgetId, decimal utilizationPercentage)
        {
            var budget = await _db.BudgetAllocations
                .Include(b => b.Department)
                    .ThenInclude(d => d!.Manager)
                .FirstOrDefaultAsync(b => b.Id == budgetId);

            if (budget?.Department?.Manager == null) return;

            await CreateAsync(new CreateNotificationDto
            {
                Title = "Budget Threshold Exceeded",
                Message = $"Budget {budget.BudgetCode} has reached {utilizationPercentage:N2}% utilization. Please review.",
                Type = NotificationType.InApp,
                Priority = NotificationPriority.Urgent,
                RecipientId = budget.Department.ManagerId!.Value,
                EntityType = nameof(BudgetAllocation),
                EntityId = budgetId.ToString(),
                ActionUrl = $"/Budget/Details/{budgetId}"
            }, 0);
        }

        private static NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                Priority = notification.Priority.ToString(),
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                ActionUrl = notification.ActionUrl,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}
