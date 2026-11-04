using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Partnerly.Integrations.Common
{
    // Polling works on localhost without a public URL (Telegram getUpdates,
    // Gmail history, Sheets rows, Drive changes).
    public class IntegrationPollingWorker : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<IntegrationPollingWorker> _logger;
        private readonly Dictionary<string, DateTime> _lastRun = new(StringComparer.Ordinal);

        public IntegrationPollingWorker(IServiceScopeFactory scopes, ILogger<IntegrationPollingWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IntegrationPollingService>();
                    var now = DateTime.UtcNow;
                    await service.PollOnceAsync(poller =>
                    {
                        if (_lastRun.TryGetValue(poller.TriggerType, out var last) && now - last < poller.Interval)
                        {
                            return false;
                        }

                        _lastRun[poller.TriggerType] = now;
                        return true;
                    }, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Tokens are never part of exception messages from our clients.
                    _logger.LogWarning(ex, "Integration polling pass failed.");
                }

                try
                {
                    await Task.Delay(Tick, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
