using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.Google.Gmail;

namespace Partnerly.Integrations.Microsoft.Outlook
{
    // New calendar events (by createdDateTime) since the saved cursor. Graph
    // change notifications need a public URL, so polling keeps it working on localhost.
    public class OutlookEventPoller : IIntegrationPoller
    {
        private const int MaxPerPass = 20;
        private const string CursorKey = "outlookEventCursor";

        private readonly OutlookApi _outlook;
        private readonly AutomationTriggerDispatcher _dispatcher;
        private readonly IAutomationClock _clock;

        public OutlookEventPoller(OutlookApi outlook, AutomationTriggerDispatcher dispatcher, IAutomationClock clock)
        {
            _outlook = outlook;
            _dispatcher = dispatcher;
            _clock = clock;
        }

        public string TriggerType => IntegrationTypeCodes.OutlookEventAdded;
        public string ConnectionKind => IntegrationKinds.Microsoft;
        public TimeSpan Interval => TimeSpan.FromSeconds(60);

        public async Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken)
        {
            var connection = target.Connection;
            var cursor = connection.GetString(CursorKey);
            if (string.IsNullOrWhiteSpace(cursor))
            {
                // First pass: start from now, never replay the existing calendar.
                connection.Set(CursorKey, await _outlook.LatestEventCreatedAsync(connection, cancellationToken)
                    ?? _clock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                return;
            }

            var events = await _outlook.NewEventsAsync(connection, cursor, cancellationToken);
            var newest = cursor;
            foreach (var payload in events.Take(MaxPerPass))
            {
                await _dispatcher.EnqueueIntegrationAsync(
                    TriggerType,
                    connection.OwnerUserId,
                    connection.Id,
                    payload,
                    config => Matches(config, payload),
                    cancellationToken);
                var created = payload["created"]?.ToString();
                if (!string.IsNullOrEmpty(created) && string.CompareOrdinal(created, newest) > 0)
                {
                    newest = created;
                }
            }

            connection.Set(CursorKey, newest);
        }

        // Only the "subject contains" filter applies to events.
        public static bool Matches(JsonElement config, JsonObject payload) => GmailPoller.Matches(config, payload);
    }
}
