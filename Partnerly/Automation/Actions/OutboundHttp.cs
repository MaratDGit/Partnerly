using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Security;

namespace Partnerly.Automation.Actions
{
    internal static class OutboundHttp
    {
        public const string ClientName = "AutomationOutbound";
        public const int MaxBytes = 256 * 1024;

        public static async Task<StepResult> SendAsync(
            IServiceProvider services,
            IPublicHttpGuard guard,
            HttpMethod method,
            string? url,
            IReadOnlyDictionary<string, string>? headers,
            string? body,
            string? signature,
            bool isDryRun,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return StepResult.Failed("HTTP URL is required.");
            }

            if (isDryRun)
            {
                return guard.IsAllowed(url)
                    ? StepResult.Skipped("dryRun")
                    : StepResult.Failed("HTTP URL is not allowed.", retryable: false);
            }

            if (!await guard.IsAllowedAsync(url, cancellationToken))
            {
                return StepResult.Failed("HTTP URL is not allowed.", retryable: false);
            }

            var factory = services.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
            if (factory == null)
            {
                return StepResult.Failed("HTTP client is not available.");
            }

            using var request = new HttpRequestMessage(method, url);
            if (headers != null)
            {
                foreach (var pair in headers)
                {
                    if (string.Equals(pair.Key, "Host", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
                }
            }

            if (!string.IsNullOrEmpty(signature))
            {
                request.Headers.TryAddWithoutValidation(WebhookSignature.HeaderName, signature);
            }

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            var client = factory.CreateClient(ClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.Content.Headers.ContentLength > MaxBytes)
            {
                return StepResult.Failed("HTTP response exceeds 256 KB.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memory = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                if (memory.Length + read > MaxBytes)
                {
                    return StepResult.Failed("HTTP response exceeds 256 KB.");
                }

                memory.Write(buffer, 0, read);
            }

            var text = Encoding.UTF8.GetString(memory.ToArray());
            return StepResult.Succeeded(new JsonObject
            {
                ["status"] = (int)response.StatusCode,
                ["body"] = text
            });
        }

        public static Dictionary<string, string> ReadHeaders(WorkflowNode node, IVariableBinder binder, JsonObject variables)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (node.Config.ValueKind != JsonValueKind.Object
                || !node.Config.TryGetProperty("headers", out var value)
                || value.ValueKind != JsonValueKind.Object)
            {
                return headers;
            }

            foreach (var property in value.EnumerateObject())
            {
                headers[property.Name] = binder.Interpolate(property.Value.ToString().Trim('"'), variables);
            }

            return headers;
        }
    }
}
