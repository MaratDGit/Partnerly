using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class DeleteRecordActionHandler : SideEffectActionHandlerBase
    {
        private readonly IVariableBinder _binder;

        public DeleteRecordActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public override string Type => AutomationTypeCodes.DeleteRecord;
        public override string DisplayName => "Delete record";

        protected override async Task<StepResult> ExecuteCoreAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var connector = RecordActionSupport.Connector(context, out var error);
            if (connector == null)
            {
                return StepResult.Failed(error ?? "Entity is required.");
            }

            if (!connector.Can("delete"))
            {
                return StepResult.Failed($"{connector.Key} does not allow delete.");
            }

            var key = RecordActionSupport.ReadObject(context.Node, _binder, context.Variables, "key");
            var deleted = await connector.DeleteAsync(key, context.Run.OwnerUserId, cancellationToken);
            return deleted ? StepResult.Succeeded() : StepResult.Failed("Delete record failed.");
        }
    }
}
