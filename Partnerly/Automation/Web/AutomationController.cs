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
    public partial class AutomationController : _BaseController
    {
        private readonly IAutomationAuthoringService _authoring;
        private readonly IAutomationRunService _runs;
        private readonly IAutomationStore _store;
        private readonly IAutomationAccessService _access;
        private readonly ISecretProtector _secrets;
        private readonly TriggerRegistry _triggers;
        private readonly ActionRegistry _actions;
        private readonly AutomationLookupService _lookups;
        private readonly AutomationFavoritesService _favorites;

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
            ActionRegistry actions,
            AutomationLookupService lookups,
            AutomationFavoritesService favorites)
            : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _authoring = authoring;
            _runs = runs;
            _store = store;
            _access = access;
            _secrets = secrets;
            _triggers = triggers;
            _actions = actions;
            _lookups = lookups;
            _favorites = favorites;
        }

        [Authorize(Policy = "Permission:automation.view")]
        public IActionResult Index()
        {
            if (Guid.TryParse(Request.Query["edit"], out var editId) && editId != Guid.Empty)
            {
                return RedirectToAction(nameof(Builder), new { id = editId });
            }

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
        public async Task<IActionResult> Create(string name, string? description, string? templateId, string? icon)
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

                await _authoring.UpdateMetaAsync(created.Id, name.Trim(), description, icon);
                created = await _authoring.GetAsync(created.Id);

                await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Automation created");
                return OkPayload(DefinitionPayload(created), nameof(Builder), new { id = created.Id });
            }
            catch (KeyNotFoundException)
            {
                return Fail(_localizer["NoTemplates"].Value, nameof(Index));
            }
        }

        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> Builder(Guid id)
        {
            try
            {
                return View(await BuildBuilderAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
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
                return OkPayload(new { success = true, id, message = _localizer["AutomationSaved"].Value }, nameof(Builder), new { id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, nameof(Builder), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.publish")]
        public async Task<IActionResult> Publish(Guid id, string? name, string? description, string? graphJson)
        {
            try
            {
                // From the builder the form carries the canvas; publish what the user sees, not the last Save.
                await SaveBuilderDraftAsync(id, name, description, graphJson);
                await _authoring.PublishAsync(id);
                await _logService.CreateLogAsync("AH", LogTypeAttribute.Information, "Automation published");
                return OkPayload(new { success = true, id, message = _localizer["AutomationPublished"].Value }, nameof(Builder), new { id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Fail(ex.Message, nameof(Builder), new { id });
            }
        }

        // Name + description only (the popup opened from a row of the Agents grid).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.edit")]
        public async Task<IActionResult> UpdateMeta(Guid id, string name, string? description, string? icon)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Fail(_localizer["AutomationNameRequired"].Value, nameof(Index));
            }

            try
            {
                var trimmed = name.Trim();
                if (trimmed.Length > 200)
                {
                    trimmed = trimmed.Substring(0, 200);
                }

                var text = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
                if (text != null && text.Length > 500)
                {
                    text = text.Substring(0, 500);
                }

                await _authoring.UpdateMetaAsync(id, trimmed, text, icon);
                await _logService.CreateLogAsync("AM", LogTypeAttribute.Information, "Automation details updated");
                return OkPayload(DefinitionPayload(await _authoring.GetAsync(id)), nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.edit")]
        public async Task<IActionResult> Rename(Guid id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Fail(_localizer["AutomationNameRequired"].Value, nameof(Index));
            }

            try
            {
                var definition = await _authoring.GetAsync(id);
                var trimmed = name.Trim();
                if (trimmed.Length > 200)
                {
                    trimmed = trimmed.Substring(0, 200);
                }

                await _authoring.UpdateMetaAsync(id, trimmed, definition.Description);
                await _logService.CreateLogAsync("AM", LogTypeAttribute.Information, "Automation renamed");
                return OkPayload(new { success = true, id, name = trimmed }, nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.edit")]
        public async Task<IActionResult> ToggleActive(Guid id, bool? isActive = null)
        {
            try
            {
                var active = await _authoring.ToggleActiveAsync(id, isActive);
                var definition = await _store.GetDefinitionAsync(id);
                var live = active && definition?.PublishedVersionId != null;
                return OkPayload(new { success = true, id, isActive = active, isLive = live, hasChanges = live && await HasUnpublishedChangesAsync(id) }, nameof(Index));
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
                return OkPayload(DefinitionPayload(copy), nameof(Builder), new { id = copy.Id });
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
        public async Task<IActionResult> Run(Guid id, string? name, string? description, string? graphJson, bool dryRun = false)
        {
            try
            {
                await SaveBuilderDraftAsync(id, name, description, graphJson);
                if (!dryRun && graphJson != null && await HasUnpublishedChangesAsync(id))
                {
                    // A live run executes the published version; running it silently with an old graph confuses users.
                    return Fail(_localizer["AutomationPublishBeforeRun"].Value, nameof(Builder), new { id });
                }

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
                return Fail(ex.Message, nameof(Builder), new { id });
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

        // Polled by the History popup: running / finished runs and the next start of one agent.
        [HttpGet]
        [Authorize(Policy = "Permission:automation.history")]
        public async Task<IActionResult> RunsData(Guid id)
        {
            try
            {
                var definition = await _authoring.GetAsync(id);
                return Json(await BuildRunMonitorAsync(definition));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
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
        public async Task<IActionResult> NodeHistoryPanel(Guid id, string nodeId, string? name)
        {
            try
            {
                var definition = await _authoring.GetAsync(id);
                var runs = VisibleRuns()
                    .Where(item => item.AutomationId == id)
                    .OrderByDescending(item => item.CreatedDate)
                    .Take(40)
                    .ToList();
                var model = new AutomationNodeHistoryViewModel
                {
                    Definition = definition,
                    NodeName = string.IsNullOrWhiteSpace(name) ? nodeId : name
                };
                foreach (var run in runs)
                {
                    var started = run.StartedAt ?? run.CreatedDate;
                    foreach (var step in (await _store.GetStepsAsync(run.Id)).Where(item => item.NodeId == nodeId).OrderByDescending(item => item.Attempt))
                    {
                        model.Rows.Add(new NodeHistoryRow
                        {
                            RunId = run.Id,
                            IsDryRun = run.IsDryRun,
                            StartedText = TimeZoneHelper.FormatIn(started, definition.TimeZoneId, "dd.MM.yyyy HH:mm:ss"),
                            StepStatus = step.Status,
                            Attempt = step.Attempt,
                            Error = step.Error
                        });
                    }
                }

                return PartialView("~/Views/Shared/Automation/_NodeHistoryPanel.cshtml", model);
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
                return OkPayload(DefinitionPayload(created), nameof(Builder), new { id = created.Id });
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
            int runTimeoutSeconds,
            string? tags = null)
        {
            try
            {
                await _authoring.UpdateTagsAsync(id, tags);
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
        public async Task<IActionResult> Catalog()
        {
            // Connections feed the "connection" select in node properties
            // (integration nodes); only the owner's own, no secrets.
            var connections = _store.QueryConnections()
                .Where(item => !item.IsDeleted && item.OwnerUserId == _access.CurrentUserId && item.Kind != "http")
                .OrderBy(item => item.Name)
                .Select(item => new { id = item.Id, name = item.Name, kind = item.Kind })
                .ToList();
            // Favorites live in the database per user. If the table is not there yet
            // (migration not applied) the editor falls back to browser storage.
            List<string>? favorites = null;
            try
            {
                favorites = await _favorites.ListAsync(_access.CurrentUserId, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync("AM", LogTypeAttribute.Warning, "Automation favorites unavailable: " + ex.GetType().Name);
            }

            return Json(new
            {
                favorites,
                triggers = _triggers.DescribeAll(HttpContext.RequestServices),
                actions = _actions.DescribeAll(HttpContext.RequestServices),
                connections,
                integrationsUrl = Url.Action("Index", "Integrations"),
                telegramChatsUrl = Url.Action("TelegramChats", "Integrations"),
                lookups = await _lookups.BuildAsync(HttpContext.RequestAborted)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> FavoriteSet(string key, bool on)
        {
            if (!AutomationFavoritesService.IsValidKey(key))
            {
                return BadRequest(new { success = false });
            }

            try
            {
                await _favorites.SetAsync(_access.CurrentUserId, key, on, HttpContext.RequestAborted);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync("AM", LogTypeAttribute.Warning, "Automation favorite not saved: " + ex.GetType().Name);
                return StatusCode(500, new { success = false });
            }
        }

        // One-time import of the browser's old favorites list (localStorage).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> FavoriteImport(string keys)
        {
            var list = (keys ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            try
            {
                await _favorites.MergeAsync(_access.CurrentUserId, list, HttpContext.RequestAborted);
                return Json(new { success = true, favorites = await _favorites.ListAsync(_access.CurrentUserId, HttpContext.RequestAborted) });
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync("AM", LogTypeAttribute.Warning, "Automation favorites import failed: " + ex.GetType().Name);
                return StatusCode(500, new { success = false });
            }
        }

        private async Task SaveBuilderDraftAsync(Guid id, string? name, string? description, string? graphJson)
        {
            if (string.IsNullOrWhiteSpace(graphJson))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                await _authoring.UpdateMetaAsync(id, name, description);
            }

            await _authoring.UpdateGraphAsync(id, WorkflowGraph.Parse(graphJson));
        }

        private async Task<bool> HasUnpublishedChangesAsync(Guid id)
        {
            var definition = await _authoring.GetAsync(id);
            if (definition.PublishedVersionId == null)
            {
                return false;
            }

            var published = await _store.GetVersionAsync(definition.PublishedVersionId.Value);
            var draft = await _authoring.GetDraftVersionAsync(id);
            return published != null && WorkflowGraphDiff.HasLogicChanges(published.GraphJson, draft.GraphJson);
        }

        // Moving cards on the canvas is not a change that needs a new publish.
        private static string Logic(string graphJson)
        {
            var graph = WorkflowGraph.Parse(graphJson);
            foreach (var node in graph.Nodes)
            {
                node.X = 0;
                node.Y = 0;
            }

            return graph.ToJson();
        }

        // Live state for the status card in the builder (also served by State()).
        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> State(Guid id)
        {
            try
            {
                var model = await BuildBuilderAsync(id, includeGraph: false);
                return Json(new
                {
                    name = model.Name,
                    isPublished = model.IsPublished,
                    isActive = model.IsActive,
                    hasChanges = model.HasUnpublishedChanges,
                    running = model.RunningCount,
                    lastStatus = model.LastRunStatus,
                    lastAt = model.LastRunAtUtc
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        private async Task<AutomationBuilderViewModel> BuildBuilderAsync(Guid id, bool includeGraph = true)
        {
            var definition = await _authoring.GetAsync(id);
            var draft = await _authoring.GetDraftVersionAsync(id);
            var runs = _store.QueryRuns()
                .Where(run => run.AutomationId == id)
                .OrderByDescending(run => run.CreatedDate)
                .Take(50)
                .ToList();
            var last = runs.FirstOrDefault();
            return new AutomationBuilderViewModel
            {
                HasUnpublishedChanges = await HasUnpublishedChangesAsync(id),
                RunningCount = runs.Count(run => run.Status is AutomationStatuses.Queued or AutomationStatuses.Running or AutomationStatuses.Waiting),
                LastRunStatus = last?.Status,
                LastRunAtUtc = last == null
                    ? null
                    : DateTime.SpecifyKind(last.EndedAt ?? last.StartedAt ?? last.CreatedDate, DateTimeKind.Utc).ToString("O"),
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description,
                Icon = definition.Icon,
                GraphJson = draft.GraphJson,
                IsPublished = definition.PublishedVersionId != null,
                IsActive = definition.IsActive
            };
        }

        private async Task<AutomationRunMonitorViewModel> BuildRunMonitorAsync(AutomationDefinition definition)
        {
            var now = DateTime.UtcNow;
            var runs = VisibleRuns()
                .Where(item => item.AutomationId == definition.Id)
                .OrderByDescending(item => item.CreatedDate)
                .Take(50)
                .ToList();
            var wakeups = await _store.GetOpenWakeupsAsync(definition.Id);
            var published = definition.PublishedVersionId != null;
            var model = new AutomationRunMonitorViewModel
            {
                NowUtc = DateTime.SpecifyKind(now, DateTimeKind.Utc),
                TimeZoneId = definition.TimeZoneId,
                IsActive = definition.IsActive,
                IsPublished = published,
                MaxConcurrentRuns = definition.MaxConcurrentRuns,
                MaxRunsPerHour = definition.MaxRunsPerHour,
                ActiveRuns = await _store.CountActiveRunsAsync(definition.Id),
                RunsLastHour = await _store.CountRunsSinceAsync(definition.Id, now.AddHours(-1))
            };

            if (published)
            {
                var triggers = await _store.GetTriggersAsync(definition.PublishedVersionId!.Value);
                model.TriggerTypes = triggers.Select(item => item.Type).Distinct().ToList();
            }

            // The next scheduled / interval start (waking runs are shown on their own row).
            var next = definition.IsActive && published
                ? wakeups.Where(item => item.RunId == null
                                        && (item.Kind == AutomationStatuses.WakeupSchedule || item.Kind == AutomationStatuses.WakeupInterval))
                    .OrderBy(item => item.DueAtUtc)
                    .FirstOrDefault()
                : null;
            if (next != null)
            {
                model.Next = new RunMonitorNext
                {
                    AtUtc = DateTime.SpecifyKind(next.DueAtUtc, DateTimeKind.Utc),
                    AtText = TimeZoneHelper.FormatIn(next.DueAtUtc, definition.TimeZoneId, "dd.MM.yyyy HH:mm:ss"),
                    Kind = next.Kind
                };
            }

            var resumeKinds = new[]
            {
                AutomationStatuses.WakeupStart, AutomationStatuses.WakeupDelay,
                AutomationStatuses.WakeupWait, AutomationStatuses.WakeupRetry
            };
            var resume = wakeups
                .Where(item => item.RunId != null && resumeKinds.Contains(item.Kind))
                .GroupBy(item => item.RunId!.Value)
                .ToDictionary(group => group.Key, group => group.Min(item => item.DueAtUtc));

            var graphs = new Dictionary<Guid, WorkflowGraph>();
            foreach (var run in runs)
            {
                if (!graphs.TryGetValue(run.VersionId, out var graph))
                {
                    var version = await _store.GetVersionAsync(run.VersionId);
                    graph = version == null ? new WorkflowGraph() : WorkflowGraph.Parse(version.GraphJson);
                    graphs[run.VersionId] = graph;
                }

                var total = graph.Nodes.Count(node => node.Kind == "action");
                // Last attempt of every card, in the order the flow was drawn.
                var latest = (await _store.GetStepsAsync(run.Id))
                    .GroupBy(step => step.NodeId)
                    .Select(group => group.OrderBy(step => step.Attempt).Last())
                    .OrderBy(step => step.StartedAt)
                    .ToList();

                var started = run.StartedAt ?? run.CreatedDate;
                model.Runs.Add(new RunMonitorRow
                {
                    Id = run.Id,
                    Status = run.Status,
                    TriggerType = run.TriggerType,
                    IsDryRun = run.IsDryRun,
                    StartedText = TimeZoneHelper.FormatIn(started, definition.TimeZoneId, "dd.MM.yyyy HH:mm:ss"),
                    StartedAtUtc = DateTime.SpecifyKind(started, DateTimeKind.Utc),
                    EndedAtUtc = run.EndedAt == null ? null : DateTime.SpecifyKind(run.EndedAt.Value, DateTimeKind.Utc),
                    CompletedSteps = CountCompleted(run.CompletedNodeIdsJson),
                    TotalSteps = total,
                    Succeeded = latest.Count(step => step.Status == AutomationStatuses.StepSucceeded),
                    Failed = latest.Count(step => step.Status == AutomationStatuses.StepFailed),
                    Skipped = latest.Count(step => step.Status == AutomationStatuses.StepSkipped),
                    Pending = latest.Count(step => step.Status is not (AutomationStatuses.StepSucceeded or AutomationStatuses.StepFailed or AutomationStatuses.StepSkipped)),
                    Steps = latest.Select(step => new RunMonitorStep
                    {
                        Name = graph.FindNode(step.NodeId)?.Name ?? step.NodeId,
                        Type = step.ActionType,
                        Status = step.Status,
                        Error = step.Error
                    }).ToList(),
                    ResumeAtUtc = resume.TryGetValue(run.Id, out var due) ? DateTime.SpecifyKind(due, DateTimeKind.Utc) : null,
                    Error = run.Error
                });
            }

            return model;
        }

        private static int CountCompleted(string? json)
        {
            try
            {
                return System.Text.Json.Nodes.JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json) is System.Text.Json.Nodes.JsonArray array
                    ? array.Count
                    : 0;
            }
            catch
            {
                return 0;
            }
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

            var version = await _store.GetVersionAsync(run.VersionId);
            var names = version == null
                ? new Dictionary<string, string>()
                : WorkflowGraph.Parse(version.GraphJson).Nodes
                    .GroupBy(node => node.Id)
                    .ToDictionary(group => group.Key, group => group.First().Name);
            return new AutomationRunDetailsViewModel
            {
                Run = run,
                Definition = definition,
                Steps = await _store.GetStepsAsync(run.Id),
                NodeNames = names
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
            var httpConnections = connections.Where(item => item.Kind == "http").ToList();
            var httpUrls = new Dictionary<Guid, string>();
            foreach (var item in httpConnections)
            {
                try
                {
                    httpUrls[item.Id] = JsonNode.Parse(_secrets.Unprotect(item.EncryptedPayload))?["baseUrl"]?.GetValue<string>() ?? string.Empty;
                }
                catch
                {
                    httpUrls[item.Id] = string.Empty;
                }
            }

            return new AutomationConnectionsViewModel
            {
                Webhooks = webhooks,
                HttpConnections = httpConnections,
                HttpUrls = httpUrls,
                IntegrationConnections = connections
                    .Where(item => item.Kind != "http" && item.OwnerUserId == _access.CurrentUserId)
                    .OrderBy(item => item.Kind)
                    .ThenBy(item => item.Name)
                    .ToList()
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
                icon = item.Icon,
                isActive = item.IsActive,
                isLive = item.IsActive && item.PublishedVersionId != null,
                hasChanges = false,
                updatedDate = Partnerly.Helpers.TimeZoneHelper.FormatIn(item.UpdatedDate, item.TimeZoneId)
            };
        }
    }
}
