using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Gmail
{
    public abstract class GmailActionBase : IntegrationActionHandlerBase
    {
        protected GmailActionBase(GmailApi gmail)
        {
            Gmail = gmail;
        }

        protected GmailApi Gmail { get; }

        public override string Group => "Gmail";
        public override string ConnectionKind => IntegrationKinds.Google;

        protected static StepResult? NeedMessageId(string id) =>
            string.IsNullOrWhiteSpace(id) ? StepResult.Failed("Message id is required.", retryable: false) : null;
    }

    public class GmailReplyAction : GmailActionBase
    {
        public GmailReplyAction(GmailApi gmail) : base(gmail) { }

        public override string Type => IntegrationTypeCodes.GmailReply;
        public override string DisplayName => "Reply to Gmail message";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Read(context, binder, "messageId");
            var missing = NeedMessageId(id);
            if (missing != null)
            {
                return missing;
            }

            var json = await Gmail.ReplyAsync(connection, id, Read(context, binder, "body"), Read(context, binder, "format") != "text", cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["messageId"] = json["id"]?.ToString(),
                ["threadId"] = json["threadId"]?.ToString(),
                ["to"] = json["to"]?.ToString()
            });
        }
    }

    public class GmailGetMessageAction : GmailActionBase
    {
        public GmailGetMessageAction(GmailApi gmail) : base(gmail) { }

        public override string Type => IntegrationTypeCodes.GmailGetMessage;
        public override string DisplayName => "Read Gmail message";
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Read(context, binder, "messageId");
            var missing = NeedMessageId(id);
            if (missing != null)
            {
                return missing;
            }

            return StepResult.Succeeded(await Gmail.GetMessageAsync(connection, id, cancellationToken));
        }
    }

    public class GmailCreateDraftAction : GmailActionBase
    {
        public GmailCreateDraftAction(GmailApi gmail) : base(gmail) { }

        public override string Type => IntegrationTypeCodes.GmailCreateDraft;
        public override string DisplayName => "Create Gmail draft";

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

            var json = await Gmail.CreateDraftAsync(
                connection,
                to,
                Read(context, binder, "subject"),
                Read(context, binder, "body"),
                Read(context, binder, "format") != "text",
                cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["draftId"] = json["id"]?.ToString(),
                ["messageId"] = (json["message"] as JsonObject)?["id"]?.ToString(),
                ["to"] = to
            });
        }
    }

    public class GmailMarkReadAction : GmailActionBase
    {
        public GmailMarkReadAction(GmailApi gmail) : base(gmail) { }

        public override string Type => IntegrationTypeCodes.GmailMarkRead;
        public override string DisplayName => "Mark Gmail message read / unread";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Read(context, binder, "messageId");
            var missing = NeedMessageId(id);
            if (missing != null)
            {
                return missing;
            }

            var unread = Read(context, binder, "state") == "unread";
            var json = await Gmail.ModifyAsync(
                connection,
                id,
                unread ? new[] { "UNREAD" } : Array.Empty<string>(),
                unread ? Array.Empty<string>() : new[] { "UNREAD" },
                cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["messageId"] = json["id"]?.ToString() ?? id, ["state"] = unread ? "unread" : "read" });
        }
    }

    public class GmailArchiveAction : GmailActionBase
    {
        public GmailArchiveAction(GmailApi gmail) : base(gmail) { }

        public override string Type => IntegrationTypeCodes.GmailArchive;
        public override string DisplayName => "Archive Gmail message";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Read(context, binder, "messageId");
            var missing = NeedMessageId(id);
            if (missing != null)
            {
                return missing;
            }

            var json = await Gmail.ModifyAsync(connection, id, Array.Empty<string>(), new[] { "INBOX" }, cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["messageId"] = json["id"]?.ToString() ?? id });
        }
    }

    public class GmailTrashAction : GmailActionBase
    {
        public GmailTrashAction(GmailApi gmail) : base(gmail) { }

        public override string Type => IntegrationTypeCodes.GmailTrash;
        public override string DisplayName => "Move Gmail message to trash";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Read(context, binder, "messageId");
            var missing = NeedMessageId(id);
            if (missing != null)
            {
                return missing;
            }

            var json = await Gmail.TrashAsync(connection, id, cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["messageId"] = json["id"]?.ToString() ?? id });
        }
    }

    public class GmailLabelAction : GmailActionBase
    {
        public GmailLabelAction(GmailApi gmail) : base(gmail) { }

        public override string Type => IntegrationTypeCodes.GmailLabel;
        public override string DisplayName => "Add or remove Gmail label";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Read(context, binder, "messageId");
            var missing = NeedMessageId(id);
            if (missing != null)
            {
                return missing;
            }

            var label = Read(context, binder, "label");
            if (string.IsNullOrWhiteSpace(label))
            {
                return StepResult.Failed("Label name is required.", retryable: false);
            }

            var remove = Read(context, binder, "mode") == "remove";
            var labelId = await Gmail.LabelIdAsync(connection, label, createIfMissing: !remove, cancellationToken);
            var json = await Gmail.ModifyAsync(
                connection,
                id,
                remove ? Array.Empty<string>() : new[] { labelId },
                remove ? new[] { labelId } : Array.Empty<string>(),
                cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["messageId"] = json["id"]?.ToString() ?? id,
                ["label"] = label,
                ["mode"] = remove ? "remove" : "add"
            });
        }
    }
}
