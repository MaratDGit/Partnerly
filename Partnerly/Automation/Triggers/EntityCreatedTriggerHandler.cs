using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class EntityCreatedTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.EntityCreated;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Entity created"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
