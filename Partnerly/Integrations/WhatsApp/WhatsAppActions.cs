using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.WhatsApp
{
    // Free text works only inside the 24-hour window after the customer wrote.
    public class WhatsAppSendTextAction : IntegrationActionHandlerBase
    {
        private readonly WhatsAppClient _client;

        public WhatsAppSendTextAction(WhatsAppClient client)
        {
            _client = client;
        }

        public override string Type => IntegrationTypeCodes.WhatsAppSendText;
        public override string DisplayName => "Send WhatsApp message";
        public override string Group => "WhatsApp";
        public override string ConnectionKind => IntegrationKinds.WhatsApp;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var text = Read(context, binder, "text");
            if (string.IsNullOrWhiteSpace(text))
            {
                return StepResult.Failed("Message text is required.", retryable: false);
            }

            var json = await _client.SendTextAsync(connection, Read(context, binder, "to"), text, cancellationToken);
            return StepResult.Succeeded(Output(json));
        }

        internal static JsonObject Output(JsonObject json) => new()
        {
            ["messageId"] = ((json["messages"] as JsonArray)?.FirstOrDefault() as JsonObject)?["id"]?.ToString(),
            ["to"] = ((json["contacts"] as JsonArray)?.FirstOrDefault() as JsonObject)?["wa_id"]?.ToString()
        };
    }

    // Business-initiated messages outside the 24-hour window need an approved template.
    public class WhatsAppSendTemplateAction : IntegrationActionHandlerBase
    {
        private readonly WhatsAppClient _client;

        public WhatsAppSendTemplateAction(WhatsAppClient client)
        {
            _client = client;
        }

        public override string Type => IntegrationTypeCodes.WhatsAppSendTemplate;
        public override string DisplayName => "Send WhatsApp template";
        public override string Group => "WhatsApp";
        public override string ConnectionKind => IntegrationKinds.WhatsApp;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var template = Read(context, binder, "template");
            if (string.IsNullOrWhiteSpace(template))
            {
                return StepResult.Failed("Template name is required.", retryable: false);
            }

            var parameters = Read(context, binder, "parameters")
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();
            var json = await _client.SendTemplateAsync(connection, Read(context, binder, "to"), template, Read(context, binder, "language"), parameters, cancellationToken);
            return StepResult.Succeeded(WhatsAppSendTextAction.Output(json));
        }
    }
}
