using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Integrations.WhatsApp;

namespace Partnerly.Integrations.Web
{
    // Public endpoints for Meta. Authenticity comes from the verify token (GET)
    // and X-Hub-Signature-256 with the app secret (POST), not from cookies.
    [AllowAnonymous]
    public class IntegrationWebhooksController : Controller
    {
        private readonly WhatsAppWebhookService _whatsApp;

        public IntegrationWebhooksController(WhatsAppWebhookService whatsApp)
        {
            _whatsApp = whatsApp;
        }

        [HttpGet("/Integrations/WhatsApp/Webhook/{connectionId:guid}")]
        public async Task<IActionResult> WhatsAppVerify(Guid connectionId)
        {
            var challenge = await _whatsApp.VerifyAsync(
                connectionId,
                Request.Query["hub.mode"],
                Request.Query["hub.verify_token"],
                Request.Query["hub.challenge"],
                HttpContext.RequestAborted);
            return challenge == null ? StatusCode(StatusCodes.Status403Forbidden) : Content(challenge, "text/plain");
        }

        [HttpPost("/Integrations/WhatsApp/Webhook/{connectionId:guid}")]
        [IgnoreAntiforgeryToken]
        [RequestSizeLimit(WhatsAppWebhookService.MaxBodyBytes)]
        public async Task<IActionResult> WhatsAppReceive(Guid connectionId)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var result = await _whatsApp.IngestAsync(
                connectionId,
                body,
                Request.Headers["X-Hub-Signature-256"],
                HttpContext.RequestAborted);
            return result switch
            {
                WhatsAppWebhookResult.Accepted => Ok(),
                WhatsAppWebhookResult.NotFound => NotFound(),
                WhatsAppWebhookResult.TooLarge => StatusCode(StatusCodes.Status413PayloadTooLarge),
                _ => Unauthorized()
            };
        }
    }
}
