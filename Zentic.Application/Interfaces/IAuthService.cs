using Zentic.Application.DTOs;

namespace Zentic.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto?> LoginAsync(string email, string password, string? ipAddress);
        Task<AppUserDto?> GetUserByIdAsync(int id);
    }
}
