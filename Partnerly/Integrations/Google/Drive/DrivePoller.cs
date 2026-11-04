using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Drive
{
    // changes.list from the saved page token. A change counts as "new file"
    // only if the file was created after the watch started (edits are ignored).
    public class DrivePoller : IIntegrationPoller
    {
        private readonly GoogleDriveApi _drive;
        private readonly AutomationTriggerDispatcher _dispatcher;
        private readonly IAutomationClock _clock;

        public DrivePoller(GoogleDriveApi drive, AutomationTriggerDispatcher dispatcher, IAutomationClock clock)
        {
            _drive = drive;
            _dispatcher = dispatcher;
            _clock = clock;
        }

        public string TriggerType => IntegrationTypeCodes.DriveFileAdded;
        public string ConnectionKind => IntegrationKinds.Google;
        public TimeSpan Interval => TimeSpan.FromSeconds(60);

        public async Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken)
        {
            var connection = target.Connection;
            var token = connection.GetString("drivePageToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                connection.Set("drivePageToken", await _drive.StartPageTokenAsync(connection, cancellationToken));
                connection.Set("driveSince", _clock.UtcNow.ToString("O"));
                return;
            }

            var since = DateTime.TryParse(connection.GetString("driveSince"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
                ? at
                : _clock.UtcNow;
            var (files, next) = await _drive.ChangesAsync(connection, token, cancellationToken);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                var id = file["id"]?.ToString() ?? string.Empty;
                var created = DateTime.TryParse(file["createdTime"]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var c)
                    ? c
                    : DateTime.MinValue;
                if (created < since || !seen.Add(id))
                {
                    continue;
                }

                await _dispatcher.EnqueueIntegrationAsync(
                    TriggerType,
                    connection.OwnerUserId,
                    connection.Id,
                    file,
                    config => Matches(config, file),
                    cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(next))
            {
                connection.Set("drivePageToken", next);
            }
        }

        // Optional filters: folderId (direct parent) and nameContains.
        public static bool Matches(JsonElement config, JsonObject file)
        {
            var folder = Read(config, "folderId");
            if (!string.IsNullOrEmpty(folder)
                && !(file["parents"] as JsonArray ?? new JsonArray()).Any(p => p?.ToString() == folder))
            {
                return false;
            }

            var name = Read(config, "nameContains");
            return string.IsNullOrEmpty(name)
                   || (file["name"]?.ToString() ?? string.Empty).Contains(name, StringComparison.OrdinalIgnoreCase);
        }

        private static string Read(JsonElement config, string name) =>
            config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value)
                ? value.ToString().Trim()
                : string.Empty;
    }
}
