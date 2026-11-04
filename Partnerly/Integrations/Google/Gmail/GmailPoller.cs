using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Gmail
{
    // users.history.list from the saved historyId. Push via Pub/Sub would need
    // a public URL, so polling keeps it working on localhost.
    public class GmailPoller : IIntegrationPoller
    {
        private const int MaxPerPass = 20;

        private readonly GmailApi _gmail;
        private readonly AutomationTriggerDispatcher _dispatcher;

        public GmailPoller(GmailApi gmail, AutomationTriggerDispatcher dispatcher)
        {
            _gmail = gmail;
            _dispatcher = dispatcher;
        }

        public string TriggerType => IntegrationTypeCodes.GmailMessageReceived;
        public string ConnectionKind => IntegrationKinds.Google;
        public TimeSpan Interval => TimeSpan.FromSeconds(60);

        public async Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken)
        {
            var connection = target.Connection;
            var historyId = connection.GetString("gmailHistoryId");
            if (string.IsNullOrWhiteSpace(historyId))
            {
                connection.Set("gmailHistoryId", await _gmail.CurrentHistoryIdAsync(connection, cancellationToken));
                return;
            }

            List<string> ids;
            string? next;
            try
            {
                (ids, next) = await _gmail.NewMessagesAsync(connection, historyId, cancellationToken);
            }
            catch (IntegrationException ex) when (ex.Message.Contains(" 404 ", StringComparison.Ordinal))
            {
                // historyId expired (Gmail keeps about a week): restart from now.
                connection.Set("gmailHistoryId", await _gmail.CurrentHistoryIdAsync(connection, cancellationToken));
                return;
            }

            foreach (var id in ids.Take(MaxPerPass))
            {
                var payload = await _gmail.GetSummaryAsync(connection, id, cancellationToken);
                await _dispatcher.EnqueueIntegrationAsync(
                    TriggerType,
                    connection.OwnerUserId,
                    connection.Id,
                    payload,
                    config => Matches(config, payload),
                    cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(next))
            {
                connection.Set("gmailHistoryId", next);
            }
        }

        public static bool Matches(JsonElement config, JsonObject payload)
        {
            return Contains(config, "fromContains", payload["from"]) && Contains(config, "subjectContains", payload["subject"]);
        }

        private static bool Contains(JsonElement config, string name, JsonNode? actual)
        {
            if (config.ValueKind != JsonValueKind.Object || !config.TryGetProperty(name, out var value))
            {
                return true;
            }

            var needle = value.ToString().Trim();
            return needle.Length == 0 || (actual?.ToString() ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase);
        }
    }
}
