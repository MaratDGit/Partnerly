using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Telegram
{
    public class TelegramSendMessageAction : IntegrationActionHandlerBase
    {
        private readonly TelegramClient _client;

        public TelegramSendMessageAction(TelegramClient client)
        {
            _client = client;
        }

        public override string Type => IntegrationTypeCodes.TelegramSendMessage;
        public override string DisplayName => "Send Telegram message";
        public override string Group => "Telegram";
        public override string ConnectionKind => IntegrationKinds.Telegram;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var chatId = TelegramChat.Resolve(Read(context, binder, "chatId"), connection);
            var text = Read(context, binder, "text");
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return StepResult.Failed("Telegram chat id is required.", retryable: false);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return StepResult.Failed("Message text is required.", retryable: false);
            }

            var parseMode = Read(context, binder, "parseMode");
            var message = await _client.SendMessageAsync(
                connection.GetString("botToken") ?? string.Empty,
                chatId,
                text.Length > 4096 ? text[..4096] : text,
                string.IsNullOrWhiteSpace(parseMode) || parseMode == "none" ? null : parseMode,
                cancellationToken);

            return StepResult.Succeeded(new JsonObject
            {
                ["messageId"] = message["message_id"]?.DeepClone(),
                ["chatId"] = chatId
            });
        }
    }

    public class TelegramSendDocumentAction : IntegrationActionHandlerBase
    {
        private readonly TelegramClient _client;

        public TelegramSendDocumentAction(TelegramClient client)
        {
            _client = client;
        }

        public override string Type => IntegrationTypeCodes.TelegramSendDocument;
        public override string DisplayName => "Send Telegram file";
        public override string Group => "Telegram";
        public override string ConnectionKind => IntegrationKinds.Telegram;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var chatId = TelegramChat.Resolve(Read(context, binder, "chatId"), connection);
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return StepResult.Failed("Telegram chat id is required.", retryable: false);
            }

            var file = await IntegrationFiles.OpenAttachmentAsync(context, Read(context, binder, "attachmentId"), cancellationToken);
            if (file == null)
            {
                return StepResult.Failed("Attachment was not found.", retryable: false);
            }

            await using (file.Stream)
            {
                var message = await _client.SendDocumentAsync(
                    connection.GetString("botToken") ?? string.Empty,
                    chatId,
                    file.Stream,
                    file.FileName,
                    Read(context, binder, "caption"),
                    cancellationToken);

                return StepResult.Succeeded(new JsonObject
                {
                    ["messageId"] = message["message_id"]?.DeepClone(),
                    ["chatId"] = chatId,
                    ["fileName"] = file.FileName
                });
            }
        }
    }

    internal static class TelegramChat
    {
        // Empty chat id falls back to the chat saved on the connection.
        public static string Resolve(string configured, IntegrationConnection connection) =>
            string.IsNullOrWhiteSpace(configured) ? connection.GetString("defaultChatId") ?? string.Empty : configured;
    }
}
