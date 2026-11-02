using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class FindRecordActionHandler : IActionHandler
    {
        private readonly IVariableBinder _binder;

        public FindRecordActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.FindRecord;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Find record"
        };

        public async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var connector = RecordActionSupport.Connector(context, out var error);
            if (connector == null)
            {
                return StepResult.Succeeded(new JsonObject { ["result"] = null });
            }

            if (!connector.Can("find"))
            {
                return StepResult.Failed($"{connector.Key} does not allow find.");
            }

            var filters = RecordActionSupport.ReadObject(context.Node, _binder, context.Variables, "filters");
            var found = await connector.FindAsync(filters, context.Run.OwnerUserId, cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["result"] = found });
        }
    }
}
