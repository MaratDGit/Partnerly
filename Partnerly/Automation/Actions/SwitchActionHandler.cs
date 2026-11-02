using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class SwitchActionHandler : IActionHandler
    {
        private readonly IVariableBinder _binder;

        public SwitchActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.Switch;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Switch"
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var raw = NodeConfig.GetString(context.Node, "value") ?? string.Empty;
            var value = _binder.Interpolate(raw, context.Variables);
            var port = string.IsNullOrEmpty(value) ? "default" : "case:" + value;
            return Task.FromResult(StepResult.Succeeded(port: port));
        }
    }
}
