using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Abstractions
{
    public interface IAutomationRunNotifier
    {
        Task RunStartedAsync(AutomationRun run, CancellationToken cancellationToken = default);
        Task StepAsync(AutomationRun run, string nodeId, string status, CancellationToken cancellationToken = default);
        Task RunFinishedAsync(AutomationRun run, CancellationToken cancellationToken = default);
    }
}
