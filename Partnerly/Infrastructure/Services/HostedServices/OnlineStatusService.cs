using Partnerly.Models;
using Microsoft.EntityFrameworkCore;

namespace Partnerly.Infrastructure.Services.HostedServices
{
    public class OnlineStatusService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceProvider _serviceProvider;

        public OnlineStatusService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(UpdateOnlineStatus, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void UpdateOnlineStatus(object? state)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (dbContext != null)
            {
                var onlineStatusAutoRefreshMinute = dbContext.SystemSettings?.FirstOrDefault()?.OnlineStatusAutoRefreshMinute ?? 10;

                var inactiveUsers = dbContext.Users?
                    .Where(u => EF.Functions.DateDiffMinute(u.LastActivity, DateTime.UtcNow) >= onlineStatusAutoRefreshMinute && u.IsOnlayn == true)
                    .ToList();
                if (inactiveUsers != null)
                {
                    foreach (var user in inactiveUsers)
                    {
                        user.IsOnlayn = false;
                    }

                    if (inactiveUsers.Any())
                    {
                        dbContext.SkipValidations = true;
                        await dbContext.SaveChangesAsync();
                        dbContext.SkipValidations = false;
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }
}
