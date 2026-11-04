using Partnerly.Automation.Registry;

namespace Partnerly.Automation.Abstractions
{
    // Lets a feature folder (for example Partnerly/Integrations) add its own
    // trigger and action types without editing the Automation registries.
    public interface IAutomationModule
    {
        void RegisterTriggers(TriggerRegistry registry);
        void RegisterActions(ActionRegistry registry);
    }
}
