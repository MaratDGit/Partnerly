using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Telegram
{
    // getUpdates long-polling replacement: short calls every few seconds,
    // cursor = last update_id + 1 stored in the connection payload.
    public class TelegramPoller : IIntegrationPoller
    {
        private readonly TelegramClient _client;
        private readonly AutomationTriggerDispatcher _dispatcher;

        public TelegramPoller(TelegramClient client, AutomationTriggerDispatcher dispatcher)
        {
            _client = client;
            _dispatcher = dispatcher;
        }

        public string TriggerType => IntegrationTypeCodes.TelegramMessageReceived;
        public string ConnectionKind => IntegrationKinds.Telegram;
        public TimeSpan Interval => TimeSpan.FromSeconds(5);

        public async Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken)
        {
            var connection = target.Connection;
            var token = connection.GetString("botToken") ?? string.Empty;
            long? offset = long.TryParse(connection.GetString("updateOffset"), out var saved) ? saved : null;

            // First pass: skip history so old messages do not start agents.
            if (offset == null)
            {
                var last = await _client.GetUpdatesAsync(token, -1, 1, cancellationToken);
                var lastId = last.Count > 0 ? last[^1]?["update_id"]?.GetValue<long>() : null;
                connection.Set("updateOffset", ((lastId ?? 0) + 1).ToString());
                return;
            }

            var updates = await _client.GetUpdatesAsync(token, offset, 50, cancellationToken);
            foreach (var update in updates.OfType<JsonObject>())
            {
                var updateId = update["update_id"]?.GetValue<long>() ?? 0;
                if (update["message"] is JsonObject message)
                {
                    var payload = ToPayload(message);
                    await _dispatcher.EnqueueIntegrationAsync(
                        TriggerType,
                        connection.OwnerUserId,
                        connection.Id,
                        payload,
                        config => Matches(config, payload),
                        cancellationToken);
                }

                connection.Set("updateOffset", (updateId + 1).ToString());
            }
        }

        public static JsonObject ToPayload(JsonObject message)
        {
            var from = message["from"] as JsonObject;
            var chat = message["chat"] as JsonObject;
            return new JsonObject
            {
                ["messageId"] = message["message_id"]?.DeepClone(),
                ["chatId"] = chat?["id"]?.ToString(),
                ["chatType"] = chat?["type"]?.ToString(),
                ["text"] = message["text"]?.ToString() ?? message["caption"]?.ToString() ?? string.Empty,
                ["fromId"] = from?["id"]?.ToString(),
                ["fromUsername"] = from?["username"]?.ToString(),
                ["fromName"] = ((from?["first_name"]?.ToString() ?? "") + " " + (from?["last_name"]?.ToString() ?? "")).Trim(),
                ["date"] = message["date"]?.DeepClone()
            };
        }

        // Optional trigger filters: chatId and textContains.
        public static bool Matches(JsonElement config, JsonObject payload)
        {
            var chat = Read(config, "chatId");
            if (!string.IsNullOrWhiteSpace(chat) && chat != payload["chatId"]?.ToString())
            {
                return false;
            }

            var contains = Read(config, "textContains");
            return string.IsNullOrWhiteSpace(contains)
                   || (payload["text"]?.ToString() ?? string.Empty).Contains(contains, StringComparison.OrdinalIgnoreCase);
        }

        private static string? Read(JsonElement config, string name) =>
            config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value) ? value.ToString() : null;
    }
}
