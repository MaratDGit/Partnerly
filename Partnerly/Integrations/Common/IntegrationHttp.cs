using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Partnerly.Integrations.Common
{
    public static class IntegrationHttp
    {
        public const string ClientName = "Partnerly.Integrations";

        public static HttpClient Create(IHttpClientFactory factory) => factory.CreateClient(ClientName);

        public static StringContent Json(JsonNode body) =>
            new(body.ToJsonString(), Encoding.UTF8, "application/json");

        public static async Task<JsonObject> ReadJsonAsync(
            HttpResponseMessage response,
            string service,
            CancellationToken cancellationToken)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            JsonObject? json = null;
            try
            {
                json = string.IsNullOrWhiteSpace(text) ? new JsonObject() : JsonNode.Parse(text) as JsonObject;
            }
            catch
            {
                json = null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var detail = ErrorText(json) ?? response.ReasonPhrase ?? "error";
                var retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
                throw new IntegrationException($"{service}: {(int)response.StatusCode} {detail}", retryable);
            }

            return json ?? new JsonObject { ["raw"] = text };
        }

        public static void Bearer(HttpRequestMessage request, string token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static string? ErrorText(JsonObject? json)
        {
            if (json == null)
            {
                return null;
            }

            // Telegram: description; Google API / Meta: error.message;
            // Google OAuth: error code string + error_description.
            if (json["description"] != null)
            {
                return json["description"]!.ToString();
            }

            if (json["error"] is JsonObject error)
            {
                return error["message"]?.ToString();
            }

            var code = json["error"]?.ToString();
            var description = json["error_description"]?.ToString();
            return code == null ? description : description == null ? code : code + ": " + description;
        }
    }
}
