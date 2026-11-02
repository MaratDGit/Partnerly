using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Abstractions
{
    public interface IAutomationScheduler
    {
        Task ArmPublishedAsync(AutomationDefinition definition, AutomationVersion version, CancellationToken cancellationToken = default);
        Task ArmTriggerAsync(AutomationDefinition definition, AutomationVersion version, WorkflowNode triggerNode, CancellationToken cancellationToken = default);
        Task<DateTime?> NextDueUtcAsync(WorkflowNode triggerNode, DateTime utcNow, string timeZoneId);
    }
}
