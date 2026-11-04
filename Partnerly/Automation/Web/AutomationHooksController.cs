using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Automation.Integration;
using Partnerly.Automation.Security;

namespace Partnerly.Automation.Web
{
    [AllowAnonymous]
    public class AutomationHooksController : Controller
    {
        private readonly AutomationWebhookIngestService _ingest;

        public AutomationHooksController(AutomationWebhookIngestService ingest)
        {
            _ingest = ingest;
        }

        [HttpPost("/Automation/hooks/{webhookPublicId}")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Incoming(string webhookPublicId, CancellationToken cancellationToken)
        {
            if (Request.ContentLength > AutomationWebhookIngestService.MaxBodyBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (Request.ContentLength == null
                && System.Text.Encoding.UTF8.GetByteCount(body) > AutomationWebhookIngestService.MaxBodyBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            var signature = Request.Headers[WebhookSignature.HeaderName].ToString();
            var result = await _ingest.IngestAsync(webhookPublicId, body, signature, cancellationToken);
            return result switch
            {
                WebhookIngestResult.Accepted => Ok(new { success = true }),
                WebhookIngestResult.Unauthorized => Unauthorized(),
                WebhookIngestResult.TooLarge => StatusCode(StatusCodes.Status413PayloadTooLarge),
                WebhookIngestResult.Busy => StatusCode(StatusCodes.Status429TooManyRequests),
                _ => NotFound()
            };
        }
    }
}
