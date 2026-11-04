using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Gmail
{
    // Sends from the connected Gmail account. The SMTP send_email action
    // (Partnerly's own mailbox) stays as it is.
    public class GmailSendAction : IntegrationActionHandlerBase
    {
        private readonly GmailApi _gmail;

        public GmailSendAction(GmailApi gmail)
        {
            _gmail = gmail;
        }

        public override string Type => IntegrationTypeCodes.GmailSend;
        public override string DisplayName => "Send Gmail";
        public override string Group => "Gmail";
        public override string ConnectionKind => IntegrationKinds.Google;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var to = Read(context, binder, "to");
            if (string.IsNullOrWhiteSpace(to) || !to.Contains('@'))
            {
                return StepResult.Failed("Recipient email is required.", retryable: false);
            }

            var json = await _gmail.SendAsync(
                connection,
                to,
                Read(context, binder, "subject"),
                Read(context, binder, "body"),
                Read(context, binder, "format") != "text",
                cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["messageId"] = json["id"]?.ToString(),
                ["threadId"] = json["threadId"]?.ToString(),
                ["to"] = to
            });
        }
    }

    public class GmailFindMessagesAction : IntegrationActionHandlerBase
    {
        private readonly GmailApi _gmail;

        public GmailFindMessagesAction(GmailApi gmail)
        {
            _gmail = gmail;
        }

        public override string Type => IntegrationTypeCodes.GmailFindMessages;
        public override string DisplayName => "Find Gmail messages";
        public override string Group => "Gmail";
        public override string ConnectionKind => IntegrationKinds.Google;
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var max = int.TryParse(Read(context, binder, "max"), out var value) ? value : 5;
            var messages = await _gmail.FindAsync(connection, Read(context, binder, "query"), max, cancellationToken);
            var array = new JsonArray(messages.Select(item => (JsonNode?)item).ToArray());
            return StepResult.Succeeded(new JsonObject
            {
                ["count"] = messages.Count,
                ["first"] = messages.Count > 0 ? messages[0].DeepClone() : null,
                ["messages"] = array
            });
        }
    }
}
