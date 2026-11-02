using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Scheduler
{
    public class WakeupLeaser
    {
        public const int LeaseSeconds = 30;
        public const int BatchSize = 10;

        private readonly IAutomationStore _store;
        private readonly IAutomationClock _clock;

        public WakeupLeaser(IAutomationStore store, IAutomationClock clock)
        {
            _store = store;
            _clock = clock;
        }

        public static string OwnerName => $"{Environment.MachineName}:{Environment.ProcessId}";

        public Task RecoverExpiredAsync(CancellationToken cancellationToken = default)
        {
            return _store.RecoverExpiredLeasesAsync(_clock.UtcNow, cancellationToken);
        }

        public Task<IReadOnlyList<AutomationWakeup>> LeaseDueAsync(CancellationToken cancellationToken = default)
        {
            var now = _clock.UtcNow;
            return _store.LeaseDueAsync(now, OwnerName, now.AddSeconds(LeaseSeconds), BatchSize, cancellationToken);
        }
    }
}
