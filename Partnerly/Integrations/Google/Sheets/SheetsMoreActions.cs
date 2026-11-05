using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Sheets
{
    public class SheetsGetRowAction : SheetsActionBase
    {
        public SheetsGetRowAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsGetRow;
        public override string DisplayName => "Get Google Sheets row";
        protected override bool Writes => false;

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

            var rows = await Sheets.ReadRowsAsync(connection, id, Read(context, binder, "sheet"), cancellationToken);
            var header = rows.Count > 0 ? rows[0] : null;
            var values = row <= rows.Count ? rows[row - 1] : new List<string>();
            var payload = GoogleSheetsApi.RowPayload(header, values, row);
            payload["found"] = row <= rows.Count;
            return StepResult.Succeeded(payload);
        }
    }

    public class SheetsReadRangeAction : SheetsActionBase
    {
        private const int MaxRows = 200;

        public SheetsReadRangeAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsReadRange;
        public override string DisplayName => "Read Google Sheets range";
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Spreadsheet(context, binder);
            var cells = Read(context, binder, "range");
            if (string.IsNullOrWhiteSpace(id) || !GoogleSheetsApi.IsCellRange(cells))
            {
                return StepResult.Failed("Spreadsheet and a range like A1:C10 are required.", retryable: false);
            }

            var rows = await Sheets.ReadRangeAsync(connection, id, Read(context, binder, "sheet"), cells, cancellationToken);
            var array = new JsonArray();
            foreach (var row in rows.Take(MaxRows))
            {
                array.Add(new JsonArray(row.Select(cell => (JsonNode?)JsonValue.Create(cell)).ToArray()));
            }

            return StepResult.Succeeded(new JsonObject
            {
                ["count"] = array.Count,
                ["first"] = array.Count > 0 ? array[0]!.DeepClone() : null,
                ["values"] = array
            });
        }
    }

    public class SheetsUpdateCellAction : SheetsActionBase
    {
        public SheetsUpdateCellAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsUpdateCell;
        public override string DisplayName => "Update Google Sheets cell";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Spreadsheet(context, binder);
            var cell = Read(context, binder, "cell");
            if (string.IsNullOrWhiteSpace(id) || !GoogleSheetsApi.IsCellRange(cell) || cell.Contains(':'))
            {
                return StepResult.Failed("Spreadsheet and one cell like B7 are required.", retryable: false);
            }

            var json = await Sheets.UpdateCellAsync(connection, id, Read(context, binder, "sheet"), cell, Read(context, binder, "value"), cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["spreadsheetId"] = id, ["range"] = json["updatedRange"]?.ToString() });
        }
    }

    public class SheetsClearRangeAction : SheetsActionBase
    {
        public SheetsClearRangeAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsClearRange;
        public override string DisplayName => "Clear Google Sheets range";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Spreadsheet(context, binder);
            var cells = Read(context, binder, "range");
            if (string.IsNullOrWhiteSpace(id) || !GoogleSheetsApi.IsCellRange(cells))
            {
                return StepResult.Failed("Spreadsheet and a range like A2:F2 are required.", retryable: false);
            }

            var json = await Sheets.ClearRangeAsync(connection, id, Read(context, binder, "sheet"), cells, cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["spreadsheetId"] = id, ["range"] = json["clearedRange"]?.ToString() });
        }
    }

    public class SheetsDeleteRowAction : SheetsActionBase
    {
        public SheetsDeleteRowAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsDeleteRow;
        public override string DisplayName => "Delete Google Sheets row";

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

            await Sheets.DeleteRowAsync(connection, id, Read(context, binder, "sheet"), row, cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["spreadsheetId"] = id, ["rowNumber"] = row });
        }
    }

    public class SheetsCreateSpreadsheetAction : SheetsActionBase
    {
        public SheetsCreateSpreadsheetAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsCreateSpreadsheet;
        public override string DisplayName => "Create Google Sheets spreadsheet";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var title = Read(context, binder, "title");
            if (string.IsNullOrWhiteSpace(title))
            {
                return StepResult.Failed("Spreadsheet title is required.", retryable: false);
            }

            var json = await Sheets.CreateSpreadsheetAsync(connection, title, Read(context, binder, "sheet"), cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["spreadsheetId"] = json["spreadsheetId"]?.ToString(),
                ["title"] = title,
                ["url"] = json["spreadsheetUrl"]?.ToString()
            });
        }
    }

    public class SheetsAddSheetAction : SheetsActionBase
    {
        public SheetsAddSheetAction(GoogleSheetsApi sheets) : base(sheets) { }

        public override string Type => IntegrationTypeCodes.SheetsAddSheet;
        public override string DisplayName => "Add Google Sheets tab";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = Spreadsheet(context, binder);
            var title = Read(context, binder, "sheet");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
            {
                return StepResult.Failed("Spreadsheet and the new tab name are required.", retryable: false);
            }

            var created = await Sheets.AddSheetAsync(connection, id, title, cancellationToken);
            created["spreadsheetId"] = id;
            return StepResult.Succeeded(created);
        }
    }
}
