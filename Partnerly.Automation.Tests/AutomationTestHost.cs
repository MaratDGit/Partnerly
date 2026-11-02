using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Hosting;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Security;

namespace Partnerly.Automation.Tests
{
    public static class AutomationTestHost
    {
        public static ServiceProvider Create(
            Guid userId,
            bool canManage = false,
            IAutomationClock? clock = null,
            IAutomationStore? store = null,
            Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddPartnerlyAutomation();
            services.AddScoped<IAutomationAccessService>(_ => new AutomationAccessService(userId, canManage));
            if (clock != null)
            {
                services.AddSingleton(clock);
            }

            if (store != null)
            {
                services.AddSingleton(store);
            }

            configure?.Invoke(services);
            return services.BuildServiceProvider();
        }
    }

    public sealed class FakeClock : IAutomationClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    }
}
