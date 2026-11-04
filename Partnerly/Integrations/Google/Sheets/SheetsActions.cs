using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Sheets
{
    public abstract class SheetsActionBase : IntegrationActionHandlerBase
    {
        protected SheetsActionBase(GoogleSheetsApi sheets)
        {
            Sheets = sheets;
        }

        protected GoogleSheetsApi Sheets { get; }

        public override string Group => "Google Sheets";
        public override string ConnectionKind => IntegrationKinds.Google;

        // One value per line; each line may use {{variables}}.
        protected static List<string> Values(ActionContext context, IVariableBinder binder) =>
            Read(context, binder, "values")
                .Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .ToList();

        protected static string Spreadsheet(ActionContext context, IVariableBinder binder) =>
            GoogleSheetsApi.SpreadsheetId(Read(context, binder, "spreadsheetId"));
    }

    public class SheetsAppendRowAction : SheetsActionBase
    {
        public SheetsAppendRowAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsAppendRow;
        public override string DisplayName => "Add row to Google Sheets";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Spreadsheet(context, binder);
            if (string.IsNullOrWhiteSpace(id))
            {
                return StepResult.Failed("Spreadsheet is required.", retryable: false);
            }

            var json = await Sheets.AppendRowAsync(connection, id, Read(context, binder, "sheet"), Values(context, binder), cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["spreadsheetId"] = id,
                ["range"] = (json["updates"] as JsonObject)?["updatedRange"]?.ToString()
            });
        }
    }

    public class SheetsUpdateRowAction : SheetsActionBase
    {
        public SheetsUpdateRowAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsUpdateRow;
        public override string DisplayName => "Update Google Sheets row";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Spreadsheet(context, binder);
            if (string.IsNullOrWhiteSpace(id) || !int.TryParse(Read(context, binder, "rowNumber"), out var row) || row < 1)
            {
                return StepResult.Failed("Spreadsheet and row number are required.", retryable: false);
            }

            var json = await Sheets.UpdateRowAsync(connection, id, Read(context, binder, "sheet"), row, Values(context, binder), cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["spreadsheetId"] = id,
                ["rowNumber"] = row,
                ["range"] = json["updatedRange"]?.ToString()
            });
        }
    }

    public class SheetsFindRowsAction : SheetsActionBase
    {
        private const int MaxRows = 50;

        public SheetsFindRowsAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsFindRows;
        public override string DisplayName => "Find rows in Google Sheets";
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Spreadsheet(context, binder);
            var column = GoogleSheetsApi.ColumnIndex(Read(context, binder, "column"));
            if (string.IsNullOrWhiteSpace(id) || column < 0)
            {
                return StepResult.Failed("Spreadsheet and column are required.", retryable: false);
            }

            var needle = Read(context, binder, "value");
            var rows = await Sheets.ReadRowsAsync(connection, id, Read(context, binder, "sheet"), cancellationToken);
            var header = rows.Count > 0 ? rows[0] : null;
            var found = new JsonArray();
            for (var i = 1; i < rows.Count && found.Count < MaxRows; i++)
            {
                var cell = column < rows[i].Count ? rows[i][column] : string.Empty;
                if (string.Equals(cell.Trim(), needle, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(GoogleSheetsApi.RowPayload(header, rows[i], i + 1));
                }
            }

            return StepResult.Succeeded(new JsonObject
            {
                ["count"] = found.Count,
                ["first"] = found.Count > 0 ? found[0]!.DeepClone() : null,
                ["rows"] = found
            });
        }
    }
}
