using System.Net.Http;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Security;

namespace Partnerly.Automation.Actions
{
    public class WebhookActionHandler : IActionHandler
    {
        private readonly IPublicHttpGuard _guard;
        private readonly IVariableBinder _binder;

        public WebhookActionHandler(IPublicHttpGuard guard, IVariableBinder binder)
        {
            _guard = guard;
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.WebhookAction;
        public bool HasSideEffects => true;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Webhook",
            HasSideEffects = true
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var url = _binder.Interpolate(NodeConfig.GetString(context.Node, "url"), context.Variables);
            var body = _binder.Interpolate(NodeConfig.GetString(context.Node, "body") ?? "{}", context.Variables);
            var secret = _binder.Interpolate(NodeConfig.GetString(context.Node, "secret"), context.Variables);
            var signature = string.IsNullOrEmpty(secret) ? null : WebhookSignature.Compute(secret, body ?? string.Empty);
            var headers = OutboundHttp.ReadHeaders(context.Node, _binder, context.Variables);
            return OutboundHttp.SendAsync(
                context.Services,
                _guard,
                HttpMethod.Post,
                url,
                headers,
                body ?? "{}",
                signature,
                context.IsDryRun,
                cancellationToken);
        }
    }
}
