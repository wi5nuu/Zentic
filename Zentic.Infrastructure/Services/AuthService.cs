using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;

        public AuthService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AuthResultDto?> LoginAsync(string email, string password, string? ipAddress)
        {
            var user = await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return new AuthResultDto { Success = false, ErrorMessage = "Invalid email or password." };

            // Update last login — non-critical, don't let failure block login
            try
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Swallow — last login update failure must not prevent authentication
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
                    DepartmentName = user.Department?.Name
                }
            };
        }

        public async Task<AppUserDto?> GetUserByIdAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.Department)
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
                DepartmentName = user.Department?.Name
            };
        }
    }
}
