using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Sheets
{
    // Reading ranges, single cells, clearing, deleting rows, new spreadsheets and tabs.
    public partial class GoogleSheetsApi
    {
        // "A1", "B2:D10", "A:C", "3:3" - letters and digits only, so nothing else reaches the URL.
        private static readonly Regex CellRange = new(@"^[A-Za-z]{0,3}\d{0,7}(:[A-Za-z]{0,3}\d{0,7})?$", RegexOptions.Compiled);

        public static bool IsCellRange(string? value) =>
            !string.IsNullOrWhiteSpace(value) && value.Length <= 30 && CellRange.IsMatch(value.Trim());

        public async Task<List<List<string>>> ReadRangeAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            string cells,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(SheetRange(sheet, cells.Trim()))}";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Google Sheets", cancellationToken);
            return (json["values"] as JsonArray ?? new JsonArray())
                .Select(row => (row as JsonArray ?? new JsonArray()).Select(cell => cell?.ToString() ?? string.Empty).ToList())
                .ToList();
        }

        public async Task<JsonObject> UpdateCellAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            string cell,
            string value,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var range = SheetRange(sheet, cell.Trim());
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(range)}?valueInputOption=USER_ENTERED";
            var body = new JsonObject { ["values"] = new JsonArray(new JsonArray(JsonValue.Create(value))) };
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Sheets", cancellationToken);
        }

        public async Task<JsonObject> ClearRangeAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            string cells,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var range = SheetRange(sheet, cells.Trim());
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(range)}:clear";
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = IntegrationHttp.Json(new JsonObject())
            }, "Google Sheets", cancellationToken);
        }

        // Tab ids are numbers; users give names, so look the tab up first (empty name = first tab).
        public async Task<int> SheetIdAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}?fields=sheets.properties(sheetId,title)";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Google Sheets", cancellationToken);
            var tabs = (json["sheets"] as JsonArray ?? new JsonArray())
                .OfType<JsonObject>()
                .Select(item => item["properties"] as JsonObject)
                .Where(item => item != null)
                .ToList();
            var wanted = string.IsNullOrWhiteSpace(sheet) ? null : sheet.Trim();
            var tab = wanted == null
                ? tabs.FirstOrDefault()
                : tabs.FirstOrDefault(item => string.Equals(item!["title"]?.ToString(), wanted, StringComparison.OrdinalIgnoreCase));
            if (tab == null)
            {
                throw new IntegrationException($"Google Sheets: sheet \"{wanted ?? "Sheet1"}\" was not found.", retryable: false);
            }

            return tab["sheetId"]?.GetValue<int>() ?? 0;
        }

        public async Task<JsonObject> DeleteRowAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string? sheet,
            int rowNumber,
            CancellationToken cancellationToken)
        {
            var sheetId = await SheetIdAsync(connection, spreadsheetId, sheet, cancellationToken);
            var body = new JsonObject
            {
                ["requests"] = new JsonArray(new JsonObject
                {
                    ["deleteDimension"] = new JsonObject
                    {
                        ["range"] = new JsonObject
                        {
                            ["sheetId"] = sheetId,
                            ["dimension"] = "ROWS",
                            ["startIndex"] = rowNumber - 1,
                            ["endIndex"] = rowNumber
                        }
                    }
                })
            };
            return await BatchUpdateAsync(connection, spreadsheetId, body, cancellationToken);
        }

        public async Task<JsonObject> AddSheetAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            string title,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var body = new JsonObject
            {
                ["requests"] = new JsonArray(new JsonObject
                {
                    ["addSheet"] = new JsonObject { ["properties"] = new JsonObject { ["title"] = title } }
                })
            };
            var json = await BatchUpdateAsync(connection, spreadsheetId, body, cancellationToken);
            var properties = ((json["replies"] as JsonArray)?.FirstOrDefault() as JsonObject)?["addSheet"]?["properties"] as JsonObject;
            return new JsonObject
            {
                ["sheetId"] = properties?["sheetId"]?.DeepClone(),
                ["title"] = properties?["title"]?.ToString() ?? title
            };
        }

        public async Task<JsonObject> CreateSpreadsheetAsync(
            IntegrationConnection connection,
            string title,
            string? firstSheet,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var body = new JsonObject { ["properties"] = new JsonObject { ["title"] = title } };
            if (!string.IsNullOrWhiteSpace(firstSheet))
            {
                body["sheets"] = new JsonArray(new JsonObject
                {
                    ["properties"] = new JsonObject { ["title"] = firstSheet.Trim() }
                });
            }

            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Sheets", cancellationToken);
        }

        private async Task<JsonObject> BatchUpdateAsync(
            IntegrationConnection connection,
            string spreadsheetId,
            JsonObject body,
            CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}:batchUpdate";
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Sheets", cancellationToken);
        }
    }
}
