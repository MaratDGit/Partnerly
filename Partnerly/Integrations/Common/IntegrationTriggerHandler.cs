using Partnerly.Automation.Abstractions;

namespace Partnerly.Integrations.Common
{
    // Integration triggers only describe themselves; pollers and webhook
    // endpoints start runs through AutomationTriggerDispatcher.EnqueueIntegrationAsync.
    public class IntegrationTriggerHandler : ITriggerHandler
    {
        private readonly string _displayName;
        private readonly string _group;
        private readonly string _connectionKind;

        public IntegrationTriggerHandler(string type, string displayName, string group, string connectionKind)
        {
            Type = type;
            _displayName = displayName;
            _group = group;
            _connectionKind = connectionKind;
        }

        public string Type { get; }

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "trigger",
            DisplayName = _displayName,
            Group = _group,
            ConnectionKind = _connectionKind
        };

        public bool CanStart(TriggerContext context) =>
            string.Equals(context.Type, Type, StringComparison.Ordinal);
    }

    public sealed class TelegramMessageTrigger : IntegrationTriggerHandler
    {
        public TelegramMessageTrigger()
            : base(IntegrationTypeCodes.TelegramMessageReceived, "Telegram message received", "Telegram", IntegrationKinds.Telegram) { }
    }

    public sealed class SheetsRowAddedTrigger : IntegrationTriggerHandler
    {
        public SheetsRowAddedTrigger()
            : base(IntegrationTypeCodes.SheetsRowAdded, "New row in Google Sheets", "Google Sheets", IntegrationKinds.Google) { }
    }

    public sealed class GmailMessageTrigger : IntegrationTriggerHandler
    {
        public GmailMessageTrigger()
            : base(IntegrationTypeCodes.GmailMessageReceived, "New Gmail message", "Gmail", IntegrationKinds.Google) { }
    }

    public sealed class DriveFileAddedTrigger : IntegrationTriggerHandler
    {
        public DriveFileAddedTrigger()
            : base(IntegrationTypeCodes.DriveFileAdded, "New file in Google Drive", "Google Drive", IntegrationKinds.Google) { }
    }

    public sealed class WhatsAppMessageTrigger : IntegrationTriggerHandler
    {
        public WhatsAppMessageTrigger()
            : base(IntegrationTypeCodes.WhatsAppMessageReceived, "WhatsApp message received", "WhatsApp", IntegrationKinds.WhatsApp) { }
    }
}
