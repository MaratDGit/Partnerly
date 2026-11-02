using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class EntityDeletedTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.EntityDeleted;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Entity deleted"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
