using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class SetVariableActionHandler : IActionHandler
    {
        private readonly IVariableBinder _binder;

        public SetVariableActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.SetVariable;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Set variable"
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var name = _binder.Interpolate(NodeConfig.GetString(context.Node, "name"), context.Variables);
            if (string.IsNullOrWhiteSpace(name) || !name.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
            {
                return Task.FromResult(StepResult.Failed("Variable name is invalid."));
            }

            var raw = NodeConfig.GetString(context.Node, "value") ?? string.Empty;
            var value = _binder.Interpolate(raw, context.Variables);
            var vars = context.Variables["vars"] as JsonObject ?? new JsonObject();
            vars[name] = value;
            context.Variables["vars"] = vars;

            return Task.FromResult(StepResult.Succeeded(new JsonObject { ["name"] = name, ["value"] = value }));
        }
    }
}
