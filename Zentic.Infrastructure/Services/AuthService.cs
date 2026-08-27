using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Application.Services;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OtpNet;
using QRCoder;
using System.Drawing;
using System.Security.Cryptography;

namespace Zentic.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IAuditService _auditService;
        private readonly IPasswordValidator _passwordValidator;
        private readonly PasswordOptions _passwordOptions;

        // Account lockout configuration
        private const int MaxFailedAttempts = 5;
        private const int LockoutDurationMinutes = 30;

        public AuthService(
            AppDbContext db, 
            IAuditService auditService,
            IPasswordValidator passwordValidator,
            IOptions<PasswordOptions> passwordOptions)
        {
            _db = db;
            _auditService = auditService;
            _passwordValidator = passwordValidator;
            _passwordOptions = passwordOptions.Value;
        }

        public async Task<AuthResultDto?> LoginAsync(string email, string password, string? ipAddress)
        {
            // Fixed: Always fetch user first, then perform timing-safe comparison
            var user = await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

            // Timing-safe check - always verify password even if user not found
            var isValidPassword = false;
            if (user != null)
            {
                isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            else
            {
                // Dummy BCrypt verify with valid hash format to prevent timing attack
                BCrypt.Net.BCrypt.Verify(password, "$2a$11$dummyhashdummyhashdummyhashdu");
            }

            // Check if user exists and password is valid
            if (user == null || !isValidPassword)
            {
                // Log failed login attempt
                if (user != null)
                {
                    await RecordFailedLoginAttempt(user, ipAddress);
                }
                
                await _auditService.LogAsync(
                    AuditEventType.LoginFailed,
                    "AppUser",
                    email,
                    0, // No userId for failed login
                    ipAddress,
                    null,
                    null,
                    $"Failed login attempt for {email}",
                    null
                );

                return new AuthResultDto { Success = false, ErrorMessage = "Invalid email or password." };
            }

            // Check if user is active
            if (!user.IsActive)
            {
                await _auditService.LogAsync(
                    AuditEventType.LoginFailed,
                    "AppUser",
                    user.Id.ToString(),
                    user.Id,
                    ipAddress,
                    null,
                    null,
                    "Login attempt for inactive account",
                    null
                );
                return new AuthResultDto { Success = false, ErrorMessage = "Account is inactive." };
            }

            // Check if account is locked
            if (user.IsLocked)
            {
                await _auditService.LogAsync(
                    AuditEventType.LoginFailed,
                    "AppUser",
                    user.Id.ToString(),
                    user.Id,
                    ipAddress,
                    null,
                    null,
                    $"Login attempt for locked account. Locked until: {user.AccountLockedUntil}",
                    null
                );
                return new AuthResultDto 
                { 
                    Success = false, 
                    ErrorMessage = $"Account is locked until {user.AccountLockedUntil:yyyy-MM-dd HH:mm}. Please try again later or contact administrator." 
                };
            }

            // Check password expiry
            if (await IsPasswordExpiredAsync(user.Id))
            {
                await _auditService.LogAsync(
                    AuditEventType.LoginFailed,
                    "AppUser",
                    user.Id.ToString(),
                    user.Id,
                    ipAddress,
                    null,
                    null,
                    "Login attempt with expired password",
                    null
                );
                return new AuthResultDto 
                { 
                    Success = false, 
                    ErrorMessage = "Your password has expired. Please reset your password." 
                };
            }

            // Successful login - reset failed attempts
            user.FailedLoginAttempts = 0;
            user.AccountLockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = ipAddress;

            try
            {
                await _db.SaveChangesAsync();
                
                // Log successful login
                await _auditService.LogAsync(
                    AuditEventType.Login,
                    "AppUser",
                    user.Id.ToString(),
                    user.Id,
                    ipAddress,
                    null,
                    null,
                    "Successful login",
                    null
                );
            }
            catch
            {
                // Non-critical - don't block login if audit fails
            }

            return new AuthResultDto
            {
                Success = true,
                User = new AppUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    EmployeeId = user.EmployeeId,
                    Position = user.Position,
                    Role = user.Role.ToString(),
                    DepartmentId = user.DepartmentId,
                    DepartmentName = user.Department?.Name,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    EmailVerified = user.EmailVerified,
                    IsLocked = user.IsLocked,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }

        private async Task RecordFailedLoginAttempt(AppUser user, string? ipAddress)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.AccountLockedUntil = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                
                await _auditService.LogAsync(
                    AuditEventType.AccountLocked,
                    "AppUser",
                    user.Id.ToString(),
                    user.Id,
                    ipAddress,
                    null,
                    null,
                    $"Account locked after {MaxFailedAttempts} failed login attempts. Locked until: {user.AccountLockedUntil}",
                    null
                );
            }

            await _db.SaveChangesAsync();
        }

        public async Task<AppUserDto?> GetUserByIdAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.Department)
                .Include(u => u.Manager)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null) return null;

            return new AppUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                EmployeeId = user.EmployeeId,
                Position = user.Position,
                Role = user.Role.ToString(),
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                ManagerId = user.ManagerId,
                ManagerName = user.Manager?.FullName,
                EmailVerified = user.EmailVerified,
                IsLocked = user.IsLocked,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<bool> UnlockAccountAsync(int userId, int adminId, string? ipAddress)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            user.FailedLoginAttempts = 0;
            user.AccountLockedUntil = null;

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                AuditEventType.AccountUnlocked,
                "AppUser",
                user.Id.ToString(),
                adminId,
                ipAddress,
                null,
                null,
                $"Account unlocked by admin {adminId}",
                null
            );

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

            if (user == null || 
                string.IsNullOrEmpty(user.PasswordResetToken) || 
                user.PasswordResetToken != resetToken ||
                user.PasswordResetExpiry == null ||
                user.PasswordResetExpiry < DateTime.UtcNow)
            {
                return false;
            }

            // Validate new password
            var validation = _passwordValidator.Validate(newPassword);
            if (!validation.IsValid)
            {
                return false;
            }

            // Check password history
            var historyValidation = _passwordValidator.ValidateWithHistory(newPassword, user.PasswordHistory);
            if (!historyValidation.IsValid)
            {
                return false;
            }

            // Hash new password
            var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);
            user.PasswordHash = newHash;
            user.AddPasswordToHistory(newHash, _passwordOptions.PasswordHistoryCount);
            user.PasswordResetToken = null;
            user.PasswordResetExpiry = null;
            user.FailedLoginAttempts = 0;
            user.AccountLockedUntil = null;
            user.LastPasswordChangeAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                AuditEventType.PasswordReset,
                "AppUser",
                user.Id.ToString(),
                user.Id,
                null,
                null,
                null,
                "Password reset via token",
                null
            );

            return true;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);

            if (user == null) 
            {
                // Return success even if user not found to prevent user enumeration
                return string.Empty;
            }

            var token = Guid.NewGuid().ToString("N");
            user.PasswordResetToken = token;
            user.PasswordResetExpiry = DateTime.UtcNow.AddHours(24);

            await _db.SaveChangesAsync();

            return token;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, string? ipAddress)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                await _auditService.LogAsync(
                    AuditEventType.PasswordChange,
                    "AppUser",
                    user.Id.ToString(),
                    userId,
                    ipAddress,
                    null,
                    null,
                    "Failed password change attempt - invalid current password",
                    null
                );
                return false;
            }

            // Validate new password
            var validation = _passwordValidator.Validate(newPassword);
            if (!validation.IsValid)
            {
                return false;
            }

            // Check password history
            var historyValidation = _passwordValidator.ValidateWithHistory(newPassword, user.PasswordHistory);
            if (!historyValidation.IsValid)
            {
                return false;
            }

            // Hash new password
            var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);
            user.PasswordHash = newHash;
            user.AddPasswordToHistory(newHash, _passwordOptions.PasswordHistoryCount);
            user.LastPasswordChangeAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                AuditEventType.PasswordChange,
                "AppUser",
                user.Id.ToString(),
                userId,
                ipAddress,
                null,
                null,
                "Password changed successfully",
                null
            );

            return true;
        }

        public async Task<PasswordValidationResult> ValidatePasswordAsync(string password)
        {
            return _passwordValidator.Validate(password);
        }

        public async Task<bool> IsPasswordExpiredAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.LastPasswordChangeAt == null) return false;

            return (DateTime.UtcNow - user.LastPasswordChangeAt.Value).TotalDays > _passwordOptions.MaxAgeDays;
        }

        public async Task<IEnumerable<string>> GetPasswordHistoryAsync(int userId, int count)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Enumerable.Empty<string>();

            return user.PasswordHistory.Take(count);
        }

        // MFA/2FA Methods
        public async Task<MfaSetupDto> EnableMfaAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) throw new InvalidOperationException("User not found.");

            if (user.MfaEnabled)
                throw new InvalidOperationException("MFA is already enabled for this user.");

            // Generate a new secret key
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretKey);

            // Generate backup codes
            var backupCodes = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                backupCodes.Add(GenerateBackupCode());
            }

            // Create QR code URI for authenticator apps
            var issuer = "Zentic";
            var account = user.Email;
            var qrCodeUri = $"otpauth://totp/{issuer}:{account}?secret={base32Secret}&issuer={issuer}&digits=6&period=30";

            // Generate QR code image
            string qrCodeBase64 = GenerateQrCodeBase64(qrCodeUri);

            // Store the secret and backup codes (but don't enable MFA yet - user must verify first)
            // Only store temporarily - will be confirmed on verification
            user.MfaSecret = base32Secret;
            user.MfaBackupCodes = backupCodes;
            user.MfaEnabled = false;
            user.MfaEnabledAt = null;

            await _db.SaveChangesAsync();

            return new MfaSetupDto
            {
                SecretKey = base32Secret,
                QrCodeUri = qrCodeUri,
                BackupCodes = backupCodes,
                ManualEntryKey = base32Secret
            };
        }

public async Task<bool> VerifyMfaCodeAsync(int userId, string code)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.MfaSecret)) return false;

            var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecret), step: 30, totpSize: 6);
            var isValid = totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);

            if (isValid)
            {
                // First successful verification - enable MFA
                if (!user.MfaEnabled)
                {
                    user.MfaEnabled = true;
                    user.MfaEnabledAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    await _auditService.LogAsync(
                        AuditEventType.MfaEnabled,
                        "AppUser",
                        user.Id.ToString(),
                        userId,
                        null,
                        null,
                        null,
                        "MFA enabled",
                        null);
                }
            }

return isValid;
        }

        public async Task<bool> DisableMfaAsync(int userId, string code)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || !user.MfaEnabled) return false;

            var isValid = await VerifyMfaCodeAsync(userId, code);
            if (!isValid) return false;

            user.MfaEnabled = false;
            user.MfaSecret = null;
            user.MfaBackupCodes = new List<string>();
            user.MfaEnabledAt = null;

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                AuditEventType.MfaDisabled,
                "AppUser",
                user.Id.ToString(),
                userId,
                null,
                null,
                null,
                "MFA disabled",
                null);

            return true;
        }

        public async Task<bool> ValidateBackupCodeAsync(int userId, string code)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || !user.MfaEnabled) return false;

            var backupCodes = user.MfaBackupCodes;
            var codeIndex = backupCodes.FindIndex(c => c.Equals(code, StringComparison.OrdinalIgnoreCase));
            
            if (codeIndex >= 0)
            {
                // Remove the used backup code
                backupCodes.RemoveAt(codeIndex);
                user.MfaBackupCodes = backupCodes;
                await _db.SaveChangesAsync();

                await _auditService.LogAsync(
                    AuditEventType.Login,
                    "AppUser",
                    user.Id.ToString(),
                    userId,
                    null,
                    null,
                    null,
                    "Login with MFA backup code",
                    null
                );

                return true;
            }

            return false;
        }

        public async Task<List<string>> GenerateBackupCodesAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || !user.MfaEnabled) throw new InvalidOperationException("User not found or MFA not enabled.");

            var newBackupCodes = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                newBackupCodes.Add(GenerateBackupCode());
            }

            user.MfaBackupCodes = newBackupCodes;
            await _db.SaveChangesAsync();

            return newBackupCodes;
        }

        public async Task<bool> IsMfaEnabledAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            return user?.MfaEnabled ?? false;
        }

        private static string GenerateBackupCode()
        {
            var random = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random);
            return Convert.ToHexString(random).ToUpperInvariant();
        }

        private static string GenerateQrCodeBase64(string qrCodeUri)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }

        public async Task<AuthResultDto?> LoginWithMfaAsync(string email, string password, string mfaCode, string? ipAddress)
        {
            // First verify password
            var loginResult = await LoginAsync(email, password, ipAddress);
            if (loginResult == null || !loginResult.Success || loginResult.User == null)
                return loginResult;

            var user = await _db.Users.FindAsync(loginResult.User.Id);
            if (user == null) return new AuthResultDto { Success = false, ErrorMessage = "User not found." };

            // Check if MFA is enabled
            if (user.MfaEnabled)
            {
                var isValidMfa = await VerifyMfaCodeAsync(user.Id, mfaCode);
                if (!isValidMfa)
                {
                    await _auditService.LogAsync(
                        AuditEventType.LoginFailed,
                        "AppUser",
                        user.Id.ToString(),
                        user.Id,
                        ipAddress,
                        null,
                        null,
                        "Invalid MFA code",
                        null
                    );
                    return new AuthResultDto { Success = false, ErrorMessage = "Invalid MFA code. Please try again." };
                }
            }

            // MFA not enabled or verified successfully
            return loginResult;
        }
    }
}
