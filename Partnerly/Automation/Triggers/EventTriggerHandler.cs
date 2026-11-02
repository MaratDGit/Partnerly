using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class EventTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.Event;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Event"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
