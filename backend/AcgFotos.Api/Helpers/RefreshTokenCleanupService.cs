using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AcgFotos.Base.Infrastructure.Repositories;

namespace AcgFotos.Api.Helpers
{
    /// <summary>
    /// Purga periódica (1x/día) de refresh tokens expirados o revocados hace más de
    /// RefreshToken:CleanupAfterDays (default 30). Mantiene la tabla acotada conservando
    /// historial reciente para auditoría.
    /// </summary>
    public class RefreshTokenCleanupService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var timer = new PeriodicTimer(Interval);
                do
                {
                    await this.CleanupAsync();
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Shutdown normal de la app.
            }
        }

        private async Task CleanupAsync()
        {
            try
            {
                var days = _configuration.GetValue<int?>("RefreshToken:CleanupAfterDays") ?? 30;
                var threshold = DateTime.UtcNow.AddDays(-days);

                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                var deleted = await repository.DeleteOlderThanAsync(threshold);

                if (deleted > 0)
                {
                    _logger.LogInformation("Purga de refresh tokens: {Deleted} filas eliminadas.", deleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la purga de refresh tokens.");
            }
        }
    }
}
