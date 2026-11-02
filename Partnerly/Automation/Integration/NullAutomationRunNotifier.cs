using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Integration
{
    public sealed class NullAutomationRunNotifier : IAutomationRunNotifier
    {
        public Task RunStartedAsync(AutomationRun run, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task StepAsync(AutomationRun run, string nodeId, string status, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RunFinishedAsync(AutomationRun run, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
