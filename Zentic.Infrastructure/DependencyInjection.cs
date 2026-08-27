using Zentic.Application.Interfaces;
using Zentic.Application.Services;
using Zentic.Domain.Entities;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Repositories;
using Zentic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

            // Password validation options
            services.Configure<PasswordOptions>(config.GetSection("PasswordOptions"));

            // Fraud detection options
            services.Configure<FraudDetectionOptions>(config.GetSection("FraudDetectionOptions"));

            // Password validator
            services.AddScoped<IPasswordValidator, PasswordValidator>();

            // Application services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IFraudDetectionService, FraudDetectionService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<IExpenseService, ExpenseService>();
            services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
            services.AddScoped<INotificationService, NotificationService>();

            // Hosted service for audit log flushing
            services.AddHostedService(sp => (IHostedService)sp.GetRequiredService<IAuditService>());

            return services;
        }

        public static async Task MigrateAndSeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Apply pending migrations (requires migrations to be created first)
            // For development without migrations, use EnsureCreatedAsync
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await db.Database.MigrateAsync();
            }
            else
            {
                // No migrations - create schema from model (development only)
                await db.Database.EnsureCreatedAsync();
            }
        }
    }
}
