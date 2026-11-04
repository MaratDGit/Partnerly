using System.Text;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Security;

namespace Partnerly.Automation.Integration
{
    public class AutomationWebhookIngestService
    {
        public const int MaxBodyBytes = 64 * 1024;

        private readonly IAutomationStore _store;
        private readonly AutomationTriggerDispatcher _dispatcher;
        private readonly ISecretProtector _secrets;

        public AutomationWebhookIngestService(
            IAutomationStore store,
            AutomationTriggerDispatcher dispatcher,
            ISecretProtector secrets)
        {
            _store = store;
            _dispatcher = dispatcher;
            _secrets = secrets;
        }

        public async Task<WebhookIngestResult> IngestAsync(
            string webhookPublicId,
            string body,
            string? signature,
            CancellationToken cancellationToken = default)
        {
            if (Encoding.UTF8.GetByteCount(body ?? string.Empty) > MaxBodyBytes)
            {
                return WebhookIngestResult.TooLarge;
            }

            var armed = await _store.GetWebhookTriggerAsync(webhookPublicId, cancellationToken);
            if (armed == null)
            {
                return WebhookIngestResult.NotFound;
            }

            string secret;
            try
            {
                secret = string.IsNullOrEmpty(armed.Trigger.WebhookSecretHash)
                    ? string.Empty
                    : _secrets.Unprotect(armed.Trigger.WebhookSecretHash);
            }
            catch
            {
                return WebhookIngestResult.Unauthorized;
            }

            if (string.IsNullOrEmpty(secret) || !WebhookSignature.Verify(secret, body ?? string.Empty, signature))
            {
                return WebhookIngestResult.Unauthorized;
            }

            JsonObject payload;
            try
            {
                payload = JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body) as JsonObject
                          ?? new JsonObject { ["raw"] = body };
            }
            catch
            {
                payload = new JsonObject { ["raw"] = body };
            }

            var queued = await _dispatcher.EnqueueArmedAsync(armed, payload, cancellationToken);
            return queued ? WebhookIngestResult.Accepted : WebhookIngestResult.Busy;
        }
    }

    public enum WebhookIngestResult
    {
        Accepted,
        NotFound,
        Unauthorized,
        TooLarge,
        Busy
    }
}
