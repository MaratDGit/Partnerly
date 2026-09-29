using Partnerly.Infrastructure.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Partnerly.Infrastructure.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> AskAsync(
            string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException(
                    "Message cannot be empty.",
                    nameof(message));

            var apiKey = _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key is not configured.");
            }

            var model = _configuration["OpenAI:Model"];

            if (string.IsNullOrWhiteSpace(model))
            {
                throw new InvalidOperationException(
                    "OpenAI model is not configured.");
            }

            var requestBody = new
            {
                model,
                input = message
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/responses");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseContent =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "OpenAI request failed. StatusCode: {StatusCode}, Response: {Response}",
                    response.StatusCode,
                    responseContent);

                throw new HttpRequestException(
                    $"OpenAI request failed with status code {(int)response.StatusCode}.");
            }

            using var json =
                JsonDocument.Parse(responseContent);

            if (!json.RootElement.TryGetProperty(
                    "output",
                    out var output))
            {
                return string.Empty;
            }

            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty(
                        "content",
                        out var content))
                    continue;

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (contentItem.TryGetProperty(
                            "text",
                            out var text))
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }
    }
}
