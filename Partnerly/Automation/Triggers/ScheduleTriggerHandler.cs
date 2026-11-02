using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class ScheduleTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.Schedule;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Schedule"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
