using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Sheets
{
    // New row = row count grew since the last pass. Cursor per spreadsheet+sheet
    // lives in payload "sheetsCursors".
    public class SheetsPoller : IIntegrationPoller
    {
        private readonly GoogleSheetsApi _sheets;
        private readonly AutomationTriggerDispatcher _dispatcher;

        public SheetsPoller(GoogleSheetsApi sheets, AutomationTriggerDispatcher dispatcher)
        {
            _sheets = sheets;
            _dispatcher = dispatcher;
        }

        public string TriggerType => IntegrationTypeCodes.SheetsRowAdded;
        public string ConnectionKind => IntegrationKinds.Google;
        public TimeSpan Interval => TimeSpan.FromSeconds(60);

        public async Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken)
        {
            var connection = target.Connection;
            var cursors = connection.Payload["sheetsCursors"] as JsonObject ?? new JsonObject();
            connection.Payload["sheetsCursors"] = cursors;

            var sheets = target.TriggerConfigs
                .Select(config => (id: GoogleSheetsApi.SpreadsheetId(Read(config, "spreadsheetId")), sheet: Read(config, "sheet")))
                .Where(item => !string.IsNullOrWhiteSpace(item.id))
                .Distinct();

            foreach (var (id, sheet) in sheets)
            {
                var key = id + "|" + sheet;
                var rows = await _sheets.ReadRowsAsync(connection, id, sheet, cancellationToken);
                var known = int.TryParse(cursors[key]?.ToString(), out var count) ? count : -1;
                cursors[key] = rows.Count;
                if (known < 0)
                {
                    continue;
                }

                var header = rows.Count > 0 ? rows[0] : null;
                for (var i = Math.Max(known, 1); i < rows.Count; i++)
                {
                    var payload = GoogleSheetsApi.RowPayload(header, rows[i], i + 1);
                    payload["spreadsheetId"] = id;
                    payload["sheet"] = sheet;
                    await _dispatcher.EnqueueIntegrationAsync(
                        TriggerType,
                        connection.OwnerUserId,
                        connection.Id,
                        payload,
                        config => GoogleSheetsApi.SpreadsheetId(Read(config, "spreadsheetId")) == id && Read(config, "sheet") == sheet,
                        cancellationToken);
                }
            }
        }

        private static string Read(JsonElement config, string name) =>
            config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value)
                ? value.ToString().Trim()
                : string.Empty;
    }
}
