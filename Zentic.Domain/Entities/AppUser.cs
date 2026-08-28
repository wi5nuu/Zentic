using System.Text.Json.Serialization;
using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    public class AppUser : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; } = UserRole.Staff;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }

        // Email verification
        public bool EmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }

        // Password reset
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }

        // Account lockout
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? AccountLockedUntil { get; set; }
        public bool IsLocked => AccountLockedUntil.HasValue && AccountLockedUntil.Value > DateTime.UtcNow;

        // Password security
        public DateTime? LastPasswordChangeAt { get; set; }
        public string? PasswordHistoryJson { get; set; }

        // MFA / 2FA
        public bool MfaEnabled { get; set; } = false;
        public string? MfaSecret { get; set; }
        public string? MfaBackupCodesJson { get; set; }
        public DateTime? MfaEnabledAt { get; set; }

        // Manager hierarchy
        public int? ManagerId { get; set; }
        public AppUser? Manager { get; set; }

        // FK
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Navigation
        public ICollection<AppUser> Subordinates { get; set; } = new List<AppUser>();
        public ICollection<ExpenseRequisition> Requisitions { get; set; } = new List<ExpenseRequisition>();
        public ICollection<ExpenseRequisition> ApprovedRequisitions { get; set; } = new List<ExpenseRequisition>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        [JsonIgnore]
        public List<string> PasswordHistory
        {
            get => string.IsNullOrEmpty(PasswordHistoryJson) 
                ? new List<string>() 
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(PasswordHistoryJson!) ?? new List<string>();
            set => PasswordHistoryJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        [JsonIgnore]
        public List<string> MfaBackupCodes
        {
            get => string.IsNullOrEmpty(MfaBackupCodesJson) 
                ? new List<string>() 
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(MfaBackupCodesJson!) ?? new List<string>();
            set => MfaBackupCodesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        public void AddPasswordToHistory(string passwordHash, int maxHistory)
        {
            if (maxHistory <= 0) return;
            var history = PasswordHistory;
            history.Insert(0, passwordHash);
            if (history.Count > maxHistory)
                history.RemoveRange(maxHistory, history.Count - maxHistory);
            PasswordHistory = history;
        }
    }
}
