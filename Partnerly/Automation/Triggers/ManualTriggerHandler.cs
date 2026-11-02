using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class ManualTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.Manual;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Manual"
        };

        public bool CanStart(TriggerContext context) => true;
    }
}
