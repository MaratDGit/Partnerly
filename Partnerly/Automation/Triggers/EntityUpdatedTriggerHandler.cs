using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class EntityUpdatedTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.EntityUpdated;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Entity updated"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
