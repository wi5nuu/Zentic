using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Repositories;
using Zentic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zentic.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            // EF Core + SQL Server
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            // UnitOfWork is implemented by AppDbContext
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

            // Generic repository
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Encryption service — key from secrets/env, never from appsettings.json
            // IV is now generated per-encryption (stored prepended to ciphertext)
            var encKey = config["Encryption:Key"] ?? Convert.ToBase64String(new byte[32]); // dev fallback only
            services.AddSingleton<IEncryptionService>(new EncryptionService(encKey));

            // Application services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IFraudDetectionService, FraudDetectionService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<IExpenseService, ExpenseService>();

            return services;
        }

        public static async Task MigrateAndSeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // EnsureCreated creates the schema from the current model if it doesn't exist.
            // This avoids the PendingModelChangesWarning that occurs when no migrations exist.
            await db.Database.EnsureCreatedAsync();
        }
    }
}
