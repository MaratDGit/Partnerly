using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Triggers
{
    public class WebhookTriggerHandler : ITriggerHandler
    {
        public string Type => AutomationTypeCodes.Webhook;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = "Webhook"
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }
}
