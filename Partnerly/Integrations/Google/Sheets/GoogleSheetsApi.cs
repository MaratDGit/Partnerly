using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Sheets
{
    public partial class GoogleSheetsApi
    {
        public const string BaseUrl = "https://sheets.googleapis.com/v4/spreadsheets";

        private static readonly Regex IdFromUrl = new(@"/spreadsheets/d/([A-Za-z0-9_-]+)", RegexOptions.Compiled);

        private readonly GoogleApiClient _google;

        public GoogleSheetsApi(GoogleApiClient google)
        {
            _google = google;
        }

        // Users paste the whole browser URL; accept it as well as a bare id.
        public static string SpreadsheetId(string value)
        {
            var match = IdFromUrl.Match(value ?? string.Empty);
            return match.Success ? match.Groups[1].Value : (value ?? string.Empty).Trim();
        }

        public static string SheetRange(string? sheet, string cells) =>
            "'" + (string.IsNullOrWhiteSpace(sheet) ? "Sheet1" : sheet.Trim()).Replace("'", "''") + "'!" + cells;

        public async Task<JsonObject> AppendRowAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            IReadOnlyList<string> values,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(SheetRange(sheet, "A1"))}:append" +
                      "?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS";
            var body = new JsonObject { ["values"] = new JsonArray(Row(values)) };
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Sheets", cancellationToken);
        }

        public async Task<JsonObject> UpdateRowAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            int rowNumber,
            IReadOnlyList<string> values,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var range = SheetRange(sheet, $"A{rowNumber}");
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(range)}?valueInputOption=USER_ENTERED";
            var body = new JsonObject { ["values"] = new JsonArray(Row(values)) };
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Sheets", cancellationToken);
        }

        public async Task<List<List<string>>> ReadRowsAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(SheetRange(sheet, "A:Z"))}";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Google Sheets", cancellationToken);
            return (json["values"] as JsonArray ?? new JsonArray())
                .Select(row => (row as JsonArray ?? new JsonArray()).Select(cell => cell?.ToString() ?? string.Empty).ToList())
                .ToList();
        }

        // Header row (row 1) turns a row into { "Email": "...", ... } for {{trigger.fields.Email}}.
        public static JsonObject RowPayload(IReadOnlyList<string>? header, IReadOnlyList<string> row, int rowNumber)
        {
            var fields = new JsonObject();
            if (header != null)
            {
                for (var i = 0; i < header.Count; i++)
                {
                    var key = header[i].Trim();
                    if (!string.IsNullOrEmpty(key) && !fields.ContainsKey(key))
                    {
                        fields[key] = i < row.Count ? row[i] : string.Empty;
                    }
                }
            }

            return new JsonObject
            {
                ["rowNumber"] = rowNumber,
                ["values"] = new JsonArray(row.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()),
                ["fields"] = fields
            };
        }

        public static int ColumnIndex(string column)
        {
            if (int.TryParse(column, out var number) && number > 0)
            {
                return number - 1;
            }

            var index = 0;
            foreach (var ch in column.Trim().ToUpperInvariant())
            {
                if (ch < 'A' || ch > 'Z')
                {
                    return -1;
                }

                index = index * 26 + (ch - 'A' + 1);
            }

            return index - 1;
        }

        private void Require(IntegrationConnection connection) =>
            _google.RequireScope(connection, GoogleOAuthService.ScopeSheets);

        private static JsonArray Row(IReadOnlyList<string> values) =>
            new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
    }
}
