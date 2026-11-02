using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class DelayTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.DelayTrigger;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Delay"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
