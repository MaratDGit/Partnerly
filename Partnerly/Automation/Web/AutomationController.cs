using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Authoring;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Automation.Registry;
using Partnerly.Automation.Security;
using Partnerly.Automation.Web.ViewModels;
using Partnerly.Helpers;
using Partnerly.Controllers;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;

namespace Partnerly.Automation.Web
{
    public class AutomationController : _BaseController
    {
        private readonly IAutomationAuthoringService _authoring;
        private readonly IAutomationRunService _runs;
        private readonly IAutomationStore _store;
        private readonly IAutomationAccessService _access;
        private readonly ISecretProtector _secrets;
        private readonly TriggerRegistry _triggers;
        private readonly ActionRegistry _actions;

        public AutomationController(
            IEventBus eventBus,
            IUserService userService,
            ICurrentUserService currentUser,
            ILogService logService,
            IStringLocalizer<SharedResource> localizer,
            ISystemSettingsService systemSettingsService,
            DataTableQueryService tableService,
            IDataTableRegistry dataTableRegistry,
            IAutomationAuthoringService authoring,
            IAutomationRunService runs,
            IAutomationStore store,
            IAutomationAccessService access,
            ISecretProtector secrets,
            TriggerRegistry triggers,
            ActionRegistry actions)
            : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _authoring = authoring;
            _runs = runs;
            _store = store;
            _access = access;
            _secrets = secrets;
            _triggers = triggers;
            _actions = actions;
        }

        [Authorize(Policy = "Permission:automation.view")]
        public IActionResult Index()
        {
            var model = new AutomationDashboardViewModel
            {
                Definitions = VisibleDefinitions().OrderByDescending(item => item.UpdatedDate).ToList(),
                Presets = AutomationTemplateCatalog.All
            };
            return View(model);
        }

        [Authorize(Policy = "Permission:automation.view")]
        public IActionResult List()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> Data([FromQuery] DataTableRequest request)
        {
            var options = _dataTableRegistry.Get(request.TableID);
            if (request.TableID != DataTableNames.automationsTable)
            {
                return BadRequest(new { success = false, message = "Unknown table." });
            }

            var result = await _tableService.ExecuteAsync(_ => VisibleDefinitions(), request, options);
            return Json(result);
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.create")]
        public IActionResult Create()
        {
            return RedirectToAction(nameof(Index), new { create = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.create")]
        public async Task<IActionResult> Create(string name, string? description, string? templateId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Fail(_localizer["AutomationNameRequired"].Value, nameof(Index));
            }

            try
            {
                AutomationDefinition created;
                if (!string.IsNullOrWhiteSpace(templateId))
                {
                    created = await _authoring.CreateFromTemplateAsync(_access.CurrentUserId, templateId);
                }
                else
                {
                    created = await _authoring.CreateDraftAsync(_access.CurrentUserId, name.Trim());
                }

                await _authoring.UpdateMetaAsync(created.Id, name.Trim(), description);
                created = await _authoring.GetAsync(created.Id);

                await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Automation created");
                return OkPayload(DefinitionPayload(created), nameof(Index), new { edit = created.Id });
            }
            catch (KeyNotFoundException)
            {
                return Fail(_localizer["NoTemplates"].Value, nameof(Index));
            }
        }

        [Authorize(Policy = "Permission:automation.view")]
        public IActionResult Builder(Guid id)
        {
            return RedirectToAction(nameof(Index), new { edit = id });
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> BuilderPanel(Guid id)
        {
            try
            {
                return PartialView("~/Views/Shared/Automation/_BuilderPanel.cshtml", await BuildBuilderAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.edit")]
        public async Task<IActionResult> BuilderSave(Guid id, string name, string? description, string graphJson)
        {
            try
            {
                await _authoring.UpdateMetaAsync(id, name, description);
                await _authoring.UpdateGraphAsync(id, WorkflowGraph.Parse(graphJson ?? "{}"));
                await _logService.CreateLogAsync("AM", LogTypeAttribute.Information, "Automation graph saved");
                return OkPayload(new { success = true, id, message = _localizer["AutomationSaved"].Value }, nameof(Index), new { edit = id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, nameof(Index), new { edit = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.publish")]
        public async Task<IActionResult> Publish(Guid id)
        {
            try
            {
                await _authoring.PublishAsync(id);
                await _logService.CreateLogAsync("AH", LogTypeAttribute.Information, "Automation published");
                return OkPayload(new { success = true, id, message = _localizer["AutomationPublished"].Value }, nameof(Index), new { edit = id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Fail(ex.Message, nameof(Index), new { edit = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.edit")]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            try
            {
                await _authoring.ToggleActiveAsync(id);
                return OkPayload(new { success = true, id }, nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.create")]
        public async Task<IActionResult> Copy(Guid id)
        {
            try
            {
                var copy = await _authoring.CopyAsync(id, _access.CurrentUserId);
                return OkPayload(DefinitionPayload(copy), nameof(Index), new { edit = copy.Id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [Authorize(Policy = "Permission:automation.delete")]
        public IActionResult Delete(Guid id)
        {
            return RedirectToAction(nameof(Index), new { delete = id });
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _authoring.DeleteAsync(id);
                await _logService.CreateLogAsync("AX", LogTypeAttribute.Information, "Automation deleted");
                return OkPayload(new { success = true, id }, nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.run")]
        public async Task<IActionResult> Run(Guid id, bool dryRun = false)
        {
            try
            {
                var run = await _authoring.EnqueueRunAsync(id, dryRun ? "test" : "manual", dryRun);
                await _logService.CreateLogAsync("AN", LogTypeAttribute.Information, dryRun ? "Automation dry-run" : "Automation run");
                return OkPayload(
                    new { success = true, id, runId = run.Id, message = _localizer["AutomationQueued"].Value },
                    nameof(Index),
                    new { run = run.Id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Fail(ex.Message, nameof(Index), new { edit = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.run")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var run = await _store.GetRunAsync(id);
            if (run == null || _access.VisibleOrNull(await _store.GetDefinitionAsync(run.AutomationId)) == null)
            {
                return NotFound();
            }

            await _runs.CancelAsync(id, _access.CurrentUserId);
            await _logService.CreateLogAsync("AL", LogTypeAttribute.Information, "Automation run cancelled");
            return OkPayload(new { success = true, runId = id }, nameof(Index), new { run = id });
        }

        [Authorize(Policy = "Permission:automation.history")]
        public IActionResult History(Guid? id)
        {
            return RedirectToAction(nameof(Index), id == null ? null : new { history = id });
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.history")]
        public async Task<IActionResult> HistoryPanel(Guid id)
        {
            try
            {
                var definition = await _authoring.GetAsync(id);
                var runs = VisibleRuns()
                    .Where(item => item.AutomationId == id)
                    .OrderByDescending(item => item.CreatedDate)
                    .Take(50)
                    .ToList();
                return PartialView("~/Views/Shared/Automation/_HistoryPanel.cshtml", new AutomationHistoryPanelViewModel
                {
                    Definition = definition,
                    Runs = runs
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.history")]
        public async Task<IActionResult> RunData([FromQuery] DataTableRequest request, Guid? automationId)
        {
            var options = _dataTableRegistry.Get(request.TableID);
            if (request.TableID != DataTableNames.automationRunsTable)
            {
                return BadRequest(new { success = false, message = "Unknown table." });
            }

            if (automationId != null)
            {
                try
                {
                    await _authoring.GetAsync(automationId.Value);
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
            }

            var result = await _tableService.ExecuteAsync(
                _ => automationId == null ? VisibleRuns() : VisibleRuns().Where(item => item.AutomationId == automationId),
                request,
                options);
            return Json(result);
        }

        [Authorize(Policy = "Permission:automation.history")]
        public IActionResult RunDetails(Guid id)
        {
            return RedirectToAction(nameof(Index), new { run = id });
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.history")]
        public async Task<IActionResult> RunDetailsPanel(Guid id)
        {
            var model = await BuildRunDetailsAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return PartialView("~/Views/Shared/Automation/_RunDetailsPanel.cshtml", model);
        }

        [Authorize(Policy = "Permission:automation.view")]
        public IActionResult Templates()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.create")]
        public async Task<IActionResult> CreateFromTemplate(string templateId)
        {
            try
            {
                var created = await _authoring.CreateFromTemplateAsync(_access.CurrentUserId, templateId);
                await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Automation from template");
                return OkPayload(DefinitionPayload(created), nameof(Index), new { edit = created.Id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [Authorize(Policy = "Permission:automation.connections")]
        public IActionResult Connections()
        {
            return RedirectToAction(nameof(Index), new { connections = 1 });
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ConnectionsPanel()
        {
            return PartialView("~/Views/Shared/Automation/_ConnectionsPanel.cshtml", await BuildConnectionsAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> CreateHttpConnection(string name, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(name) || !PublicHttpGuard.IsAllowed(baseUrl))
            {
                return Fail(_localizer["AutomationInvalidConnection"].Value, nameof(Index), new { connections = 1 });
            }

            var now = DateTime.UtcNow;
            var payload = new JsonObject { ["baseUrl"] = baseUrl.Trim() }.ToJsonString();
            await _store.AddConnectionAsync(new AutomationConnection
            {
                Id = Guid.NewGuid(),
                OwnerUserId = _access.CurrentUserId,
                Name = name.Trim(),
                Kind = "http",
                EncryptedPayload = _secrets.Protect(payload),
                CreatedBy = _access.CurrentUserId,
                CreatedDate = now,
                UpdatedBy = _access.CurrentUserId,
                UpdatedDate = now
            });
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "HTTP connection created");
            return OkPayload(new { success = true, message = _localizer["AutomationSaved"].Value, reload = "connections" }, nameof(Index), new { connections = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> DeleteHttpConnection(Guid id)
        {
            var connection = await _store.GetConnectionAsync(id);
            if (connection == null
                || connection.IsDeleted
                || (!_access.CanManage && connection.OwnerUserId != _access.CurrentUserId))
            {
                return NotFound();
            }

            connection.IsDeleted = true;
            connection.UpdatedBy = _access.CurrentUserId;
            connection.UpdatedDate = DateTime.UtcNow;
            await _store.UpdateConnectionAsync(connection);
            return OkPayload(new { success = true, reload = "connections" }, nameof(Index), new { connections = 1 });
        }

        [Authorize(Policy = "Permission:automation.view")]
        public IActionResult Settings(Guid? id)
        {
            return RedirectToAction(nameof(Index), id == null ? null : new { settings = id });
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> SettingsPanel(Guid id)
        {
            try
            {
                var definition = await _authoring.GetAsync(id);
                return PartialView("~/Views/Shared/Automation/_SettingsPanel.cshtml", new AutomationSettingsViewModel { Definition = definition });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.edit")]
        public async Task<IActionResult> SettingsSave(
            Guid id,
            string timeZoneId,
            int maxConcurrentRuns,
            int maxRunsPerHour,
            int retryLimit,
            int stepTimeoutSeconds,
            int runTimeoutSeconds)
        {
            try
            {
                await _authoring.UpdateSettingsAsync(
                    id,
                    timeZoneId,
                    maxConcurrentRuns,
                    maxRunsPerHour,
                    retryLimit,
                    stepTimeoutSeconds,
                    runTimeoutSeconds);
                await _logService.CreateLogAsync("AM", LogTypeAttribute.Information, "Automation settings saved");
                return OkPayload(new { success = true, id, message = _localizer["AutomationSaved"].Value }, nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public IActionResult Catalog()
        {
            return Json(new
            {
                triggers = _triggers.DescribeAll(HttpContext.RequestServices),
                actions = _actions.DescribeAll(HttpContext.RequestServices)
            });
        }

        private async Task<AutomationBuilderViewModel> BuildBuilderAsync(Guid id)
        {
            var definition = await _authoring.GetAsync(id);
            var draft = await _authoring.GetDraftVersionAsync(id);
            return new AutomationBuilderViewModel
            {
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description,
                GraphJson = draft.GraphJson,
                IsPublished = definition.PublishedVersionId != null,
                IsActive = definition.IsActive
            };
        }

        private async Task<AutomationRunDetailsViewModel?> BuildRunDetailsAsync(Guid id)
        {
            var run = await _store.GetRunAsync(id);
            if (run == null)
            {
                return null;
            }

            var definition = _access.VisibleOrNull(await _store.GetDefinitionAsync(run.AutomationId));
            if (definition == null)
            {
                return null;
            }

            return new AutomationRunDetailsViewModel
            {
                Run = run,
                Definition = definition,
                Steps = await _store.GetStepsAsync(run.Id)
            };
        }

        private async Task<AutomationConnectionsViewModel> BuildConnectionsAsync()
        {
            var webhooks = new List<AutomationConnectionItem>();
            var definitions = VisibleDefinitions().Where(item => item.PublishedVersionId != null).ToList();
            foreach (var definition in definitions)
            {
                var triggers = await _store.GetTriggersAsync(definition.PublishedVersionId!.Value);
                foreach (var trigger in triggers.Where(item => !string.IsNullOrEmpty(item.WebhookPublicId)))
                {
                    string? secret = null;
                    try
                    {
                        secret = string.IsNullOrEmpty(trigger.WebhookSecretHash)
                            ? null
                            : _secrets.Unprotect(trigger.WebhookSecretHash);
                    }
                    catch
                    {
                        secret = null;
                    }

                    webhooks.Add(new AutomationConnectionItem
                    {
                        Name = trigger.Type,
                        AutomationName = definition.Name,
                        Url = $"{Request.Scheme}://{Request.Host}/Automation/hooks/{trigger.WebhookPublicId}",
                        Secret = secret
                    });
                }
            }

            var connections = _store.QueryConnections().Where(item =>
                !item.IsDeleted && (_access.CanManage || item.OwnerUserId == _access.CurrentUserId));
            return new AutomationConnectionsViewModel
            {
                Webhooks = webhooks,
                HttpConnections = connections.ToList()
            };
        }

        private IQueryable<AutomationDefinition> VisibleDefinitions()
        {
            var query = _store.QueryDefinitions().Where(item => !item.IsDeleted);
            if (!_access.CanManage)
            {
                var owner = _access.CurrentUserId;
                query = query.Where(item => item.OwnerUserId == owner);
            }

            return query;
        }

        private IQueryable<AutomationRun> VisibleRuns()
        {
            var query = _store.QueryRuns();
            if (!_access.CanManage)
            {
                var owner = _access.CurrentUserId;
                query = query.Where(item => item.OwnerUserId == owner);
            }

            return query;
        }

        private bool IsAjax()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult OkPayload(object payload, string action, object? routeValues = null)
        {
            if (IsAjax())
            {
                return Json(payload);
            }

            var message = payload.GetType().GetProperty("message")?.GetValue(payload) as string;
            if (!string.IsNullOrEmpty(message))
            {
                TempData["ToastMessage"] = message;
            }

            return RedirectToAction(action, routeValues);
        }

        private IActionResult Fail(string message, string action, object? routeValues = null)
        {
            if (IsAjax())
            {
                return BadRequest(new { success = false, message });
            }

            TempData["ToastMessage"] = message;
            return RedirectToAction(action, routeValues);
        }

        private object DefinitionPayload(AutomationDefinition item)
        {
            return new
            {
                success = true,
                id = item.Id,
                name = item.Name,
                description = item.Description ?? string.Empty,
                isActive = item.IsActive,
                updatedDate = item.UpdatedDate.ToString("dd.MM.yyyy HH:mm")
            };
        }
    }
}
