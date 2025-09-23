using Partnerly.Models;
using Microsoft.EntityFrameworkCore;

namespace Partnerly.Infrastructure.Services.HostedServices
{
    public class OnlineStatusService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;

        public OnlineStatusService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(UpdateOnlineStatus, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        private void UpdateOnlineStatus(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var inactiveUsers = dbContext.Users
                    .Where(u => EF.Functions.DateDiffMinute(u.LastActivity, DateTime.UtcNow) >= 5 && u.IsOnlayn == true)
                    .ToList();

                foreach (var user in inactiveUsers)
                {
                    user.IsOnlayn = false;
                }

                if (inactiveUsers.Any())
                    dbContext.SaveChanges();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
