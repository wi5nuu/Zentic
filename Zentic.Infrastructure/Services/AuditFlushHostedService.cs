using Zentic.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Zentic.Infrastructure.Services
{
    public class AuditFlushHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditFlushHostedService> _logger;

        public AuditFlushHostedService(IServiceScopeFactory scopeFactory, ILogger<AuditFlushHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            if (auditService is AuditService auditServiceImpl)
            {
                auditServiceImpl.StartFlushTimer();
            }
            _logger.LogInformation("Audit flush timer started");
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            if (auditService is AuditService auditServiceImpl)
            {
                await auditServiceImpl.StopFlushTimerAsync(cancellationToken);
            }
            _logger.LogInformation("Audit flush timer stopped and pending logs flushed");
        }
    }
}