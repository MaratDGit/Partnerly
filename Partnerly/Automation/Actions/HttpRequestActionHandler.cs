using System.Net.Http;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;

namespace Partnerly.Automation.Actions
{
    public class HttpRequestActionHandler : IActionHandler
    {
        private static readonly HashSet<string> Methods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "POST", "PUT", "PATCH", "DELETE"
        };

        private readonly IPublicHttpGuard _guard;
        private readonly IVariableBinder _binder;

        public HttpRequestActionHandler(IPublicHttpGuard guard, IVariableBinder binder)
        {
            _guard = guard;
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.HttpRequest;
        public bool HasSideEffects => true;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "HTTP request",
            HasSideEffects = true
        };

        public async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var methodRaw = _binder.Interpolate(
                NodeConfig.GetString(context.Node, "method") ?? "GET",
                context.Variables);
            if (string.IsNullOrWhiteSpace(methodRaw) || !Methods.Contains(methodRaw))
            {
                return StepResult.Failed("HTTP method is not allowed.");
            }

            var method = new HttpMethod(methodRaw.ToUpperInvariant());
            var url = _binder.Interpolate(NodeConfig.GetString(context.Node, "url"), context.Variables);
            var resolved = await ResolveUrlAsync(context, url, cancellationToken);
            if (!resolved.ok)
            {
                return StepResult.Failed(resolved.error ?? "HTTP URL is required.");
            }

            var body = method == HttpMethod.Get || method == HttpMethod.Delete
                ? null
                : _binder.Interpolate(NodeConfig.GetString(context.Node, "body"), context.Variables);
            var headers = OutboundHttp.ReadHeaders(context.Node, _binder, context.Variables);
            return await OutboundHttp.SendAsync(
                context.Services,
                _guard,
                method,
                resolved.url,
                headers,
                body,
                signature: null,
                context.IsDryRun,
                cancellationToken);
        }

        private async Task<(bool ok, string? url, string? error)> ResolveUrlAsync(
            ActionContext context,
            string? url,
            CancellationToken cancellationToken)
        {
            var connectionRaw = _binder.Interpolate(
                NodeConfig.GetString(context.Node, "connectionId"),
                context.Variables);
            if (string.IsNullOrWhiteSpace(connectionRaw))
            {
                return string.IsNullOrWhiteSpace(url)
                    ? (false, null, "HTTP URL is required.")
                    : (true, url, null);
            }

            if (!Guid.TryParse(connectionRaw, out var connectionId))
            {
                return (false, null, "HTTP connection was not found.");
            }

            var store = context.Services.GetService(typeof(IAutomationStore)) as IAutomationStore;
            var connection = store == null ? null : await store.GetConnectionAsync(connectionId, cancellationToken);
            if (connection == null
                || connection.IsDeleted
                || connection.OwnerUserId != context.Run.OwnerUserId)
            {
                return (false, null, "HTTP connection was not found.");
            }

            var protector = context.Services.GetService(typeof(ISecretProtector)) as ISecretProtector;
            string baseUrl;
            try
            {
                var json = protector == null
                    ? connection.EncryptedPayload
                    : protector.Unprotect(connection.EncryptedPayload);
                baseUrl = JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json)?["baseUrl"]?.GetValue<string>()
                          ?? string.Empty;
            }
            catch
            {
                return (false, null, "HTTP connection payload is invalid.");
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return (false, null, "HTTP connection has no base URL.");
            }

            if (string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return (true, string.IsNullOrWhiteSpace(url) ? baseUrl : url, null);
            }

            return (true, baseUrl.TrimEnd('/') + "/" + url.TrimStart('/'), null);
        }
    }
}
