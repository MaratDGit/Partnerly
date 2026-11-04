using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.WhatsApp
{
    // Meta calls a public URL, so on localhost this needs a tunnel
    // (Cloudflare Tunnel / ngrok). Outgoing messages work without it.
    public class WhatsAppWebhookService
    {
        public const int MaxBodyBytes = 256 * 1024;

        private readonly IIntegrationConnectionService _connections;
        private readonly AutomationTriggerDispatcher _dispatcher;

        public WhatsAppWebhookService(IIntegrationConnectionService connections, AutomationTriggerDispatcher dispatcher)
        {
            _connections = connections;
            _dispatcher = dispatcher;
        }

        public async Task<string?> VerifyAsync(Guid connectionId, string? mode, string? verifyToken, string? challenge, CancellationToken cancellationToken = default)
        {
            var connection = await _connections.GetAsync(connectionId, cancellationToken);
            if (connection == null || connection.Kind != IntegrationKinds.WhatsApp || mode != "subscribe")
            {
                return null;
            }

            var expected = connection.GetString("verifyToken") ?? string.Empty;
            return expected.Length > 0 && string.Equals(expected, verifyToken, StringComparison.Ordinal) ? challenge : null;
        }

        public async Task<WhatsAppWebhookResult> IngestAsync(Guid connectionId, string body, string? signature, CancellationToken cancellationToken = default)
        {
            if (Encoding.UTF8.GetByteCount(body ?? string.Empty) > MaxBodyBytes)
            {
                return WhatsAppWebhookResult.TooLarge;
            }

            var connection = await _connections.GetAsync(connectionId, cancellationToken);
            if (connection == null || connection.Kind != IntegrationKinds.WhatsApp)
            {
                return WhatsAppWebhookResult.NotFound;
            }

            // Signature first, parse later.
            if (!WhatsAppClient.VerifySignature(connection.GetString("appSecret") ?? string.Empty, body ?? string.Empty, signature))
            {
                return WhatsAppWebhookResult.Unauthorized;
            }

            foreach (var payload in Messages(body ?? string.Empty))
            {
                await _dispatcher.EnqueueIntegrationAsync(
                    IntegrationTypeCodes.WhatsAppMessageReceived,
                    connection.OwnerUserId,
                    connection.Id,
                    payload,
                    config => Matches(config, payload),
                    cancellationToken);
            }

            return WhatsAppWebhookResult.Accepted;
        }

        public static List<JsonObject> Messages(string body)
        {
            var result = new List<JsonObject>();
            JsonObject? root;
            try
            {
                root = JsonNode.Parse(body) as JsonObject;
            }
            catch
            {
                return result;
            }

            foreach (var entry in (root?["entry"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
            {
                foreach (var change in (entry["changes"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                {
                    var value = change["value"] as JsonObject;
                    var contact = (value?["contacts"] as JsonArray)?.FirstOrDefault() as JsonObject;
                    foreach (var message in (value?["messages"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                    {
                        result.Add(new JsonObject
                        {
                            ["messageId"] = message["id"]?.ToString(),
                            ["from"] = message["from"]?.ToString(),
                            ["name"] = (contact?["profile"] as JsonObject)?["name"]?.ToString(),
                            ["type"] = message["type"]?.ToString(),
                            ["text"] = (message["text"] as JsonObject)?["body"]?.ToString()
                                       ?? (message["button"] as JsonObject)?["text"]?.ToString()
                                       ?? string.Empty,
                            ["timestamp"] = message["timestamp"]?.ToString()
                        });
                    }
                }
            }

            return result;
        }

        public static bool Matches(JsonElement config, JsonObject payload)
        {
            var contains = config.ValueKind == JsonValueKind.Object && config.TryGetProperty("textContains", out var value)
                ? value.ToString().Trim()
                : string.Empty;
            return contains.Length == 0
                   || (payload["text"]?.ToString() ?? string.Empty).Contains(contains, StringComparison.OrdinalIgnoreCase);
        }
    }

    public enum WhatsAppWebhookResult
    {
        Accepted,
        NotFound,
        Unauthorized,
        TooLarge
    }
}
