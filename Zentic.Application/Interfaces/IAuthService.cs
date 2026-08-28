using Zentic.Application.DTOs;
using Zentic.Application.Services;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto?> LoginAsync(string email, string password, string? ipAddress, CancellationToken cancellationToken = default);
        Task<AuthResultDto?> LoginWithMfaAsync(string email, string password, string mfaCode, string? ipAddress, CancellationToken cancellationToken = default);
        Task<AppUserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> UnlockAccountAsync(int userId, int adminId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword, CancellationToken cancellationToken = default);
        Task<string> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, string? ipAddress, CancellationToken cancellationToken = default);
        Task<PasswordValidationResult> ValidatePasswordAsync(string password, CancellationToken cancellationToken = default);
        Task<bool> IsPasswordExpiredAsync(int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetPasswordHistoryAsync(int userId, int count, CancellationToken cancellationToken = default);
        
        // MFA/2FA methods
        Task<MfaSetupDto> EnableMfaAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> VerifyMfaCodeAsync(int userId, string code, CancellationToken cancellationToken = default);
        Task<bool> DisableMfaAsync(int userId, string code, CancellationToken cancellationToken = default);
        Task<bool> ValidateBackupCodeAsync(int userId, string code, CancellationToken cancellationToken = default);
        Task<List<string>> GenerateBackupCodesAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> IsMfaEnabledAsync(int userId, CancellationToken cancellationToken = default);
    }
}
