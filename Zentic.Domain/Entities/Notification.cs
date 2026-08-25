using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        // Recipient
        public int RecipientId { get; set; }
        public AppUser? Recipient { get; set; }

        // Status
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public bool IsSent { get; set; } = false;
        public DateTime? SentAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public string? ErrorMessage { get; set; }

        // Related entity
        public string? EntityType { get; set; } // ExpenseRequisition, Vendor, etc
        public string? EntityId { get; set; }
        public string? ActionUrl { get; set; }

        // Email/SMS specific
        public string? EmailTo { get; set; }
        public string? EmailSubject { get; set; }
        public string? SmsTo { get; set; }

        // Template
        public string? TemplateName { get; set; }
        public string? TemplateData { get; set; } // JSON
    }
}
