using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Abstractions
{
    // Tells the owner that a run failed. Optional: the engine works without it (tests, no notification service).
    public interface IRunFailureAlert
    {
        Task RunFailedAsync(AutomationRun run, AutomationDefinition definition, WorkflowGraph graph, CancellationToken cancellationToken = default);
    }
}
