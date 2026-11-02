using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class IntervalTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.Interval;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Interval"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
