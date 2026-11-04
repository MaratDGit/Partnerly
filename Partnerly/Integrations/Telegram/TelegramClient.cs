using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Telegram
{
    // Bot API over plain REST (plan D1). The token is part of the URL, so
    // request URLs must never be logged.
    public class TelegramClient
    {
        public const string BaseUrl = "https://api.telegram.org";

        private readonly IHttpClientFactory _http;

        public TelegramClient(IHttpClientFactory http)
        {
            _http = http;
        }

        public async Task<JsonObject> GetMeAsync(string token, CancellationToken cancellationToken = default)
        {
            return Result(await CallAsync(token, "getMe", new JsonObject(), cancellationToken));
        }

        public async Task<JsonObject> SendMessageAsync(
            string token,
            string chatId,
            string text,
            string? parseMode,
            CancellationToken cancellationToken = default)
        {
            var body = new JsonObject { ["chat_id"] = chatId, ["text"] = text };
            if (!string.IsNullOrWhiteSpace(parseMode))
            {
                body["parse_mode"] = parseMode;
            }

            return Result(await CallAsync(token, "sendMessage", body, cancellationToken));
        }

        public async Task<JsonObject> SendDocumentAsync(
            string token,
            string chatId,
            Stream file,
            string fileName,
            string? caption,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(chatId), "chat_id");
            if (!string.IsNullOrWhiteSpace(caption))
            {
                form.Add(new StringContent(caption), "caption");
            }

            var content = new StreamContent(file);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(content, "document", fileName);

            var client = IntegrationHttp.Create(_http);
            using var response = await client.PostAsync(MethodUrl(token, "sendDocument"), form, cancellationToken);
            try
            {
                return Result(await IntegrationHttp.ReadJsonAsync(response, "Telegram", cancellationToken));
            }
            catch (IntegrationException ex)
            {
                throw Explain(ex);
            }
        }

        public async Task<JsonArray> GetUpdatesAsync(
            string token,
            long? offset,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var body = new JsonObject
            {
                ["limit"] = limit,
                ["timeout"] = 0,
                ["allowed_updates"] = new JsonArray("message")
            };
            if (offset != null)
            {
                body["offset"] = offset.Value;
            }

            var json = await CallAsync(token, "getUpdates", body, cancellationToken);
            return json["result"] as JsonArray ?? new JsonArray();
        }

        private async Task<JsonObject> CallAsync(string token, string method, JsonObject body, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new IntegrationException("Telegram bot token is missing.", retryable: false);
            }

            var client = IntegrationHttp.Create(_http);
            using var response = await client.PostAsync(MethodUrl(token, method), IntegrationHttp.Json(body), cancellationToken);
            try
            {
                return await IntegrationHttp.ReadJsonAsync(response, "Telegram", cancellationToken);
            }
            catch (IntegrationException ex)
            {
                throw Explain(ex);
            }
        }

        // Telegram's own texts are short and cryptic; add what the user should do.
        public static IntegrationException Explain(IntegrationException error)
        {
            var text = error.Message;
            string? hint = null;
            if (text.Contains("chat not found", StringComparison.OrdinalIgnoreCase))
            {
                hint = "Chat not found. Write any message to the bot first, then pick the chat with Recent chats on the Integrations page (chat id is a number, not @username).";
            }
            else if (text.Contains("chat_id is empty", StringComparison.OrdinalIgnoreCase))
            {
                hint = "Chat id is empty. Fill Chat id in the step or set a default chat on the Integrations page.";
            }
            else if (text.Contains("bot was blocked", StringComparison.OrdinalIgnoreCase))
            {
                hint = "The user blocked the bot. Ask them to press Start in the bot again.";
            }
            else if (text.Contains("can't parse entities", StringComparison.OrdinalIgnoreCase))
            {
                hint = "Formatting error. Choose Plain text in the step or fix the HTML / Markdown.";
            }
            else if (text.Contains("message text is empty", StringComparison.OrdinalIgnoreCase))
            {
                hint = "Message text is empty after variables were filled in.";
            }

            return hint == null ? error : new IntegrationException(text + " — " + hint, retryable: false);
        }

        private static string MethodUrl(string token, string method) => $"{BaseUrl}/bot{token}/{method}";

        private static JsonObject Result(JsonObject json)
        {
            if (json["ok"]?.GetValue<bool>() != true)
            {
                throw new IntegrationException("Telegram: " + (json["description"]?.ToString() ?? "request failed"), retryable: false);
            }

            return json["result"] as JsonObject ?? new JsonObject();
        }
    }
}
