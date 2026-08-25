using Zentic.Application.DTOs;
using Zentic.Application.Services;

namespace Zentic.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto?> LoginAsync(string email, string password, string? ipAddress);
        Task<AuthResultDto?> LoginWithMfaAsync(string email, string password, string mfaCode, string? ipAddress);
        Task<AppUserDto?> GetUserByIdAsync(int id);
        Task<bool> UnlockAccountAsync(int userId, int adminId, string? ipAddress);
        Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, string? ipAddress);
        Task<PasswordValidationResult> ValidatePasswordAsync(string password);
        Task<bool> IsPasswordExpiredAsync(int userId);
        Task<IEnumerable<string>> GetPasswordHistoryAsync(int userId, int count);
        
        // MFA/2FA methods
        Task<MfaSetupDto> EnableMfaAsync(int userId);
        Task<bool> VerifyMfaCodeAsync(int userId, string code);
        Task<bool> DisableMfaAsync(int userId, string code);
        Task<bool> ValidateBackupCodeAsync(int userId, string code);
        Task<List<string>> GenerateBackupCodesAsync(int userId);
        Task<bool> IsMfaEnabledAsync(int userId);
    }
}
