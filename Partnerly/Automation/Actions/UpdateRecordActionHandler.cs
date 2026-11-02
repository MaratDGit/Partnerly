using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class UpdateRecordActionHandler : SideEffectActionHandlerBase
    {
        private readonly IVariableBinder _binder;

        public UpdateRecordActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public override string Type => AutomationTypeCodes.UpdateRecord;
        public override string DisplayName => "Update record";

        protected override async Task<StepResult> ExecuteCoreAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var connector = RecordActionSupport.Connector(context, out var error);
            if (connector == null)
            {
                return StepResult.Failed(error ?? "Entity is required.");
            }

            if (!connector.Can("update"))
            {
                return StepResult.Failed($"{connector.Key} does not allow update.");
            }

            var key = RecordActionSupport.ReadObject(context.Node, _binder, context.Variables, "key");
            var fields = RecordActionSupport.ReadObject(context.Node, _binder, context.Variables, "fields");
            var updated = await connector.UpdateAsync(key, fields, context.Run.OwnerUserId, cancellationToken);
            return updated == null
                ? StepResult.Failed("Update record failed.")
                : StepResult.Succeeded(updated);
        }
    }
}
