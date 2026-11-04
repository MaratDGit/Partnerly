using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.WhatsApp
{
    // Meta WhatsApp Cloud API (plan D3).
    public class WhatsAppClient
    {
        public const string BaseUrl = "https://graph.facebook.com/v20.0";

        private readonly IHttpClientFactory _http;

        public WhatsAppClient(IHttpClientFactory http)
        {
            _http = http;
        }

        public async Task<JsonObject> GetPhoneNumberAsync(IntegrationConnection connection, CancellationToken cancellationToken = default)
        {
            var url = $"{BaseUrl}/{Uri.EscapeDataString(PhoneNumberId(connection))}?fields=display_phone_number,verified_name";
            return await SendAsync(connection, new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        }

        public Task<JsonObject> SendTextAsync(IntegrationConnection connection, string to, string text, CancellationToken cancellationToken = default)
        {
            return SendMessageAsync(connection, new JsonObject
            {
                ["messaging_product"] = "whatsapp",
                ["to"] = NormalizePhone(to),
                ["type"] = "text",
                ["text"] = new JsonObject { ["body"] = text, ["preview_url"] = false }
            }, cancellationToken);
        }

        public Task<JsonObject> SendTemplateAsync(
            IntegrationConnection connection,
            string to,
            string template,
            string language,
            IReadOnlyList<string> parameters,
            CancellationToken cancellationToken = default)
        {
            var body = new JsonObject
            {
                ["name"] = template,
                ["language"] = new JsonObject { ["code"] = string.IsNullOrWhiteSpace(language) ? "en_US" : language }
            };
            if (parameters.Count > 0)
            {
                body["components"] = new JsonArray(new JsonObject
                {
                    ["type"] = "body",
                    ["parameters"] = new JsonArray(parameters
                        .Select(value => (JsonNode?)new JsonObject { ["type"] = "text", ["text"] = value })
                        .ToArray())
                });
            }

            return SendMessageAsync(connection, new JsonObject
            {
                ["messaging_product"] = "whatsapp",
                ["to"] = NormalizePhone(to),
                ["type"] = "template",
                ["template"] = body
            }, cancellationToken);
        }

        // X-Hub-Signature-256: "sha256=" + hex(HMAC-SHA256(appSecret, rawBody)).
        public static bool VerifySignature(string appSecret, string body, string? header)
        {
            if (string.IsNullOrWhiteSpace(appSecret) || string.IsNullOrWhiteSpace(header) || !header.StartsWith("sha256=", StringComparison.Ordinal))
            {
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body ?? string.Empty))).ToLowerInvariant();
            var actual = header["sha256=".Length..].Trim().ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual));
        }

        public static string Sign(string appSecret, string body)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        }

        public static string NormalizePhone(string phone) =>
            new((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        private async Task<JsonObject> SendMessageAsync(IntegrationConnection connection, JsonObject body, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(body["to"]?.ToString()))
            {
                throw new IntegrationException("WhatsApp recipient phone is required.", retryable: false);
            }

            var url = $"{BaseUrl}/{Uri.EscapeDataString(PhoneNumberId(connection))}/messages";
            return await SendAsync(connection, new HttpRequestMessage(HttpMethod.Post, url) { Content = IntegrationHttp.Json(body) }, cancellationToken);
        }

        private async Task<JsonObject> SendAsync(IntegrationConnection connection, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (request)
            {
                var token = connection.GetString("accessToken");
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new IntegrationException("WhatsApp access token is missing.", retryable: false);
                }

                IntegrationHttp.Bearer(request, token);
                using var response = await IntegrationHttp.Create(_http).SendAsync(request, cancellationToken);
                return await IntegrationHttp.ReadJsonAsync(response, "WhatsApp", cancellationToken);
            }
        }

        private static string PhoneNumberId(IntegrationConnection connection)
        {
            var id = connection.GetString("phoneNumberId");
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new IntegrationException("WhatsApp phone number id is missing.", retryable: false);
            }

            return id;
        }
    }
}
