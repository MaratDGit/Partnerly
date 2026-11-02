using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class CreateRecordActionHandler : SideEffectActionHandlerBase
    {
        private readonly IVariableBinder _binder;

        public CreateRecordActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public override string Type => AutomationTypeCodes.CreateRecord;
        public override string DisplayName => "Create record";

        protected override async Task<StepResult> ExecuteCoreAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var connector = RecordActionSupport.Connector(context, out var error);
            if (connector == null)
            {
                return StepResult.Failed(error ?? "Entity is required.");
            }

            if (!connector.Can("create"))
            {
                return StepResult.Failed($"{connector.Key} does not allow create.");
            }

            var fields = RecordActionSupport.ReadObject(context.Node, _binder, context.Variables, "fields");
            var created = await connector.CreateAsync(fields, context.Run.OwnerUserId, cancellationToken);
            return created == null
                ? StepResult.Failed("Create record failed.")
                : StepResult.Succeeded(created);
        }
    }
}
