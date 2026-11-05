using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.Google.Gmail;

namespace Partnerly.Integrations.Microsoft.Outlook
{
    // New inbox mail since the saved cursor (receivedDateTime). Graph change
    // notifications need a public URL, so polling keeps it working on localhost.
    public class OutlookPoller : IIntegrationPoller
    {
        private const int MaxPerPass = 20;
        private const string CursorKey = "outlookCursor";

        private readonly OutlookApi _outlook;
        private readonly AutomationTriggerDispatcher _dispatcher;
        private readonly IAutomationClock _clock;

        public OutlookPoller(OutlookApi outlook, AutomationTriggerDispatcher dispatcher, IAutomationClock clock)
        {
            _outlook = outlook;
            _dispatcher = dispatcher;
            _clock = clock;
        }

        public string TriggerType => IntegrationTypeCodes.OutlookMessageReceived;
        public string ConnectionKind => IntegrationKinds.Microsoft;
        public TimeSpan Interval => TimeSpan.FromSeconds(60);

        public async Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken)
        {
            var connection = target.Connection;
            var cursor = connection.GetString(CursorKey);
            if (string.IsNullOrWhiteSpace(cursor))
            {
                // First pass: start from now, never replay the old inbox.
                connection.Set(CursorKey, await _outlook.LatestReceivedAsync(connection, cancellationToken)
                    ?? _clock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                return;
            }

            var messages = await _outlook.NewMessagesAsync(connection, cursor, cancellationToken);
            var newest = cursor;
            foreach (var payload in messages.Take(MaxPerPass))
            {
                await _dispatcher.EnqueueIntegrationAsync(
                    TriggerType,
                    connection.OwnerUserId,
                    connection.Id,
                    payload,
                    config => Matches(config, payload),
                    cancellationToken);
                var received = payload["date"]?.ToString();
                if (!string.IsNullOrEmpty(received) && string.CompareOrdinal(received, newest) > 0)
                {
                    newest = received;
                }
            }

            connection.Set(CursorKey, newest);
        }

        // Same "from contains" / "subject contains" filters as the Gmail trigger.
        public static bool Matches(JsonElement config, JsonObject payload) => GmailPoller.Matches(config, payload);
    }
}
