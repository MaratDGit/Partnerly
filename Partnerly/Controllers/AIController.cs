using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<AIController> _logger;
        private readonly ISystemSettingsService _systemSettingsService;

        public AIController(
            IOpenAIService openAIService,
            ILogger<AIController> logger,
            ISystemSettingsService systemSettingsService)
        {
            _openAIService = openAIService;
            _logger = logger;
            _systemSettingsService = systemSettingsService;
        }

        [HttpPost]
        [Authorize(Policy = "Permission:ai.use")]
        public async Task<IActionResult> Ask([FromBody] AIRequestModel model, CancellationToken cancellationToken)
        {
            var settings = await _systemSettingsService.GetCurrentAsync();
            if (settings?.IsAiAssistantEnabled == false)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = ErrorMessages.AiAssistantDisabled
                });
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Message is required."
                });
            }

            try
            {
                var answer = await _openAIService.AskAsync(model.Message, cancellationToken);

                return Json(new
                {
                    success = true,
                    answer
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new
                {
                    success = false,
                    message = "Request was cancelled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing AI request.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to process the AI request."
                });
            }
        }
    }

    public class AIRequestModel
    {
        public string Message { get; set; } = string.Empty;
    }
}