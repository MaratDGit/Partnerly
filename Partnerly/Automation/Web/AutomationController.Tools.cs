using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Automation.Web.ViewModels;
using Partnerly.Descriptors.Attributes;
using Partnerly.Helpers;

namespace Partnerly.Automation.Web
{
    // Extra tools around an agent: re-run / resume, test one card, versions, 7-day numbers, export / import.
    public partial class AutomationController
    {
        private const string ExportFormat = "partnerly.agent";
        private const long MaxImportBytes = 1024 * 1024;

        // ---- run again / continue a failed run ----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.run")]
        public async Task<IActionResult> Rerun(Guid id)
        {
            var run = await _store.GetRunAsync(id);
            if (run == null || _access.VisibleOrNull(await _store.GetDefinitionAsync(run.AutomationId)) == null)
            {
                return NotFound();
            }

            var created = await _runs.RerunAsync(id);
            await _logService.CreateLogAsync("AN", LogTypeAttribute.Information, "Automation run repeated");
            return OkPayload(
                new { success = true, runId = created.Id, message = _localizer["ToolsRerunQueued"].Value },
                nameof(Index),
                new { run = created.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.run")]
        public async Task<IActionResult> Resume(Guid id)
        {
            var run = await _store.GetRunAsync(id);
            if (run == null || _access.VisibleOrNull(await _store.GetDefinitionAsync(run.AutomationId)) == null)
            {
                return NotFound();
            }

            if (!await _runs.ResumeFailedAsync(id))
            {
                return Fail(_localizer["ToolsResumeOnlyFailed"].Value, nameof(Index), new { run = id });
            }

            await _logService.CreateLogAsync("AN", LogTypeAttribute.Information, "Automation run resumed");
            return OkPayload(
                new { success = true, runId = id, message = _localizer["ToolsResumeQueued"].Value },
                nameof(Index),
                new { run = id });
        }

        // ---- test one card -----------------------------------------------------
        // Runs the card in test mode: cards that change something (mail, records, HTTP...) do not run for real,
        // the answer shows what they would receive. Values come from the latest run of this agent.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.run")]
        public async Task<IActionResult> TestNode(Guid id, string? nodeJson)
        {
            var definition = _access.VisibleOrNull(await _store.GetDefinitionAsync(id));
            if (definition == null)
            {
                return NotFound();
            }

            WorkflowNode? node;
            try
            {
                node = string.IsNullOrWhiteSpace(nodeJson) || nodeJson.Length > 64 * 1024
                    ? null
                    : JsonSerializer.Deserialize<WorkflowNode>(nodeJson, WorkflowGraph.JsonOptions);
            }
            catch (JsonException)
            {
                node = null;
            }

            if (node == null || node.Kind != "action" || !_actions.IsKnown(node.Type))
            {
                return BadRequest(new { success = false, message = _localizer["ToolsTestBadCard"].Value });
            }

            var binder = HttpContext.RequestServices.GetRequiredService<IVariableBinder>();
            var lastRun = VisibleRuns()
                .Where(item => item.AutomationId == id)
                .OrderByDescending(item => item.CreatedDate)
                .Select(item => item.ContextJson)
                .FirstOrDefault();
            JsonObject variables;
            try
            {
                variables = JsonNode.Parse(string.IsNullOrWhiteSpace(lastRun) ? "{}" : lastRun) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                variables = new JsonObject();
            }

            foreach (var key in new[] { "trigger", "steps", "vars" })
            {
                variables[key] ??= new JsonObject();
            }

            var preview = node.Config.ValueKind == JsonValueKind.Object
                ? Interpolated(JsonNode.Parse(node.Config.GetRawText()), binder, variables)
                : null;

            var run = new AutomationRun
            {
                Id = Guid.NewGuid(),
                AutomationId = id,
                OwnerUserId = definition.OwnerUserId,
                IsDryRun = true,
                Status = AutomationStatuses.Running
            };
            var context = new Partnerly.Automation.Abstractions.ActionContext
            {
                Run = run,
                Node = node,
                Variables = variables,
                IsDryRun = true,
                UtcNow = DateTime.UtcNow,
                Services = HttpContext.RequestServices
            };

            StepResult result;
            try
            {
                var handler = _actions.Resolve(HttpContext.RequestServices, node.Type);
                result = await handler.ExecuteAsync(context, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                result = StepResult.Failed(ex.Message);
            }

            return Json(new
            {
                success = true,
                status = result.Status,
                error = result.Error,
                port = result.Port,
                output = result.Output,
                config = preview,
                usedLastRun = !string.IsNullOrWhiteSpace(lastRun)
            });
        }

        private static JsonNode? Interpolated(JsonNode? node, IVariableBinder binder, JsonObject variables)
        {
            switch (node)
            {
                case JsonObject obj:
                    var copy = new JsonObject();
                    foreach (var pair in obj)
                    {
                        copy[pair.Key] = Interpolated(pair.Value, binder, variables);
                    }

                    return copy;
                case JsonArray array:
                    var list = new JsonArray();
                    foreach (var item in array)
                    {
                        list.Add(Interpolated(item, binder, variables));
                    }

                    return list;
                case JsonValue value when value.TryGetValue<string>(out var text):
                    return binder.Interpolate(text, variables);
                default:
                    return node == null ? null : JsonNode.Parse(node.ToJsonString());
            }
        }

        // ---- versions, 7-day numbers --------------------------------------------
        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> ToolsPanel(Guid id)
        {
            AutomationDefinition definition;
            try
            {
                definition = await _authoring.GetAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            var model = new AutomationToolsViewModel { Definition = definition };
            var versions = _store.QueryVersions()
                .Where(item => item.AutomationId == id)
                .OrderByDescending(item => item.Number)
                .Take(30)
                .ToList();
            foreach (var version in versions)
            {
                var cards = 0;
                try
                {
                    cards = WorkflowGraph.Parse(version.GraphJson).Nodes.Count;
                }
                catch (JsonException)
                {
                    // an unreadable old version is still listed
                }

                model.Versions.Add(new ToolsVersionRow
                {
                    Id = version.Id,
                    Number = version.Number,
                    Cards = cards,
                    IsDraft = version.Id == definition.DraftVersionId,
                    IsLive = version.Id == definition.PublishedVersionId,
                    WhenText = TimeZoneHelper.FormatIn(version.PublishedAt ?? version.CreatedDate, definition.TimeZoneId, "dd.MM.yyyy HH:mm")
                });
            }

            return PartialView("~/Views/Shared/Automation/_ToolsPanel.cshtml", model);
        }

        // Header of the side section: the short facts about this one agent.
        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> SideSummary(Guid id)
        {
            AutomationDefinition definition;
            try
            {
                definition = await _authoring.GetAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            string changedBy = string.Empty;
            string changedAt = string.Empty;
            if (definition.UpdatedBy is Guid changedById && changedById != Guid.Empty)
            {
                var editor = await _userService.GetUserByIDAsync(changedById);
                changedBy = editor == null ? string.Empty : $"{editor.FirstName} {editor.LastName}".Trim();
                changedAt = TimeZoneHelper.FormatIn(definition.UpdatedDate, definition.TimeZoneId, "dd.MM.yyyy HH:mm");
            }

            int? versionNumber = null;
            if (definition.PublishedVersionId is Guid publishedId)
            {
                versionNumber = (await _store.GetVersionAsync(publishedId))?.Number;
            }

            var since = DateTime.UtcNow.Date.AddDays(-6);
            var runs = VisibleRuns()
                .Where(item => item.AutomationId == id && !item.IsDryRun && item.CreatedDate >= since)
                .Select(item => new { item.CreatedDate, item.Status, item.StartedAt, item.EndedAt })
                .ToList();
            var days = Enumerable.Range(0, 7).Select(offset =>
            {
                var date = since.AddDays(offset);
                var ofDay = runs.Where(item => item.CreatedDate.Date == date).ToList();
                return new
                {
                    label = date.ToString("dd.MM"),
                    ok = ofDay.Count(item => item.Status == AutomationStatuses.Succeeded),
                    failed = ofDay.Count(item => item.Status == AutomationStatuses.Failed),
                    other = ofDay.Count(item => item.Status != AutomationStatuses.Succeeded && item.Status != AutomationStatuses.Failed)
                };
            }).ToList();
            var finished = runs.Where(item => item.StartedAt != null && item.EndedAt != null).ToList();
            var last = VisibleRuns()
                .Where(item => item.AutomationId == id)
                .OrderByDescending(item => item.CreatedDate)
                .Select(item => new { item.Status, item.CreatedDate, item.IsDryRun })
                .FirstOrDefault();

            return Json(new
            {
                name = definition.Name,
                icon = definition.Icon,
                timeZone = definition.TimeZoneId,
                isActive = definition.IsActive,
                isPublished = definition.PublishedVersionId != null,
                version = versionNumber,
                changedBy,
                changedAt,
                total = runs.Count,
                succeeded = runs.Count(item => item.Status == AutomationStatuses.Succeeded),
                failed = runs.Count(item => item.Status == AutomationStatuses.Failed),
                averageSeconds = finished.Count == 0 ? 0 : Math.Round(finished.Average(item => (item.EndedAt!.Value - item.StartedAt!.Value).TotalSeconds), 1),
                days,
                lastRunStatus = last?.Status,
                lastRunDry = last?.IsDryRun ?? false,
                lastRunAt = last == null ? string.Empty : TimeZoneHelper.FormatIn(last.CreatedDate, definition.TimeZoneId, "dd.MM.yyyy HH:mm")
            });
        }

        // Puts an older version into the draft. The live (published) version is not touched until Publish.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.edit")]
        public async Task<IActionResult> RestoreVersion(Guid id, Guid versionId)
        {
            try
            {
                await _authoring.GetAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            var version = await _store.GetVersionAsync(versionId);
            if (version == null || version.AutomationId != id)
            {
                return NotFound();
            }

            await _authoring.UpdateGraphAsync(id, WorkflowGraph.Parse(version.GraphJson));
            await _logService.CreateLogAsync("AN", LogTypeAttribute.Information, "Automation version restored to draft");
            return OkPayload(
                new { success = true, id, reload = "page", message = _localizer["ToolsVersionRestored"].Value },
                nameof(Builder),
                new { id });
        }

        // Rename a web address connection or change its address. Agents that use it send to the new address
        // from their next step on (they point at the connection, not at the address).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> UpdateHttpConnection(Guid id, string name, string baseUrl)
        {
            var connection = await _store.GetConnectionAsync(id);
            if (connection == null
                || connection.IsDeleted
                || connection.Kind != "http"
                || (!_access.CanManage && connection.OwnerUserId != _access.CurrentUserId))
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200 || !PublicHttpGuard.IsAllowed(baseUrl))
            {
                return Fail(_localizer["AutomationInvalidConnection"].Value, nameof(Index), new { connections = 1 });
            }

            // keep whatever else the payload holds, only the address changes
            JsonObject payload;
            try
            {
                payload = JsonNode.Parse(_secrets.Unprotect(connection.EncryptedPayload)) as JsonObject ?? new JsonObject();
            }
            catch
            {
                payload = new JsonObject();
            }

            payload["baseUrl"] = baseUrl.Trim();
            connection.Name = name.Trim();
            connection.EncryptedPayload = _secrets.Protect(payload.ToJsonString());
            connection.UpdatedBy = _access.CurrentUserId;
            connection.UpdatedDate = DateTime.UtcNow;
            await _store.UpdateConnectionAsync(connection);
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "HTTP connection updated");
            return OkPayload(new { success = true, message = _localizer["AutomationSaved"].Value, reload = "connections" }, nameof(Index), new { connections = 1 });
        }

        // ---- export / import -------------------------------------------------------
        [HttpGet]
        [Authorize(Policy = "Permission:automation.view")]
        public async Task<IActionResult> Export(Guid id)
        {
            AutomationDefinition definition;
            AutomationVersion draft;
            try
            {
                definition = await _authoring.GetAsync(id);
                draft = await _authoring.GetDraftVersionAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            var graph = JsonNode.Parse(WorkflowGraph.Parse(draft.GraphJson).ToJson());
            var package = new JsonObject
            {
                ["format"] = ExportFormat,
                ["version"] = 1,
                ["name"] = definition.Name,
                ["description"] = definition.Description,
                ["icon"] = definition.Icon,
                ["graph"] = graph
            };
            var safeName = new string(definition.Name.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
            var bytes = System.Text.Encoding.UTF8.GetBytes(package.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return File(bytes, "application/json", (string.IsNullOrEmpty(safeName) ? "agent" : safeName) + ".agent.json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.create")]
        public async Task<IActionResult> Import(IFormFile? file)
        {
            if (file == null || file.Length == 0 || file.Length > MaxImportBytes)
            {
                return BadRequest(new { success = false, message = _localizer["ToolsImportBad"].Value });
            }

            JsonObject? package;
            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                package = JsonNode.Parse(await reader.ReadToEndAsync()) as JsonObject;
            }
            catch (JsonException)
            {
                package = null;
            }

            if (package == null
                || package["format"]?.GetValue<string>() != ExportFormat
                || package["graph"] is not JsonObject graphNode)
            {
                return BadRequest(new { success = false, message = _localizer["ToolsImportBad"].Value });
            }

            WorkflowGraph graph;
            try
            {
                graph = WorkflowGraph.Parse(graphNode.ToJsonString());
            }
            catch (JsonException)
            {
                return BadRequest(new { success = false, message = _localizer["ToolsImportBad"].Value });
            }

            // Connections belong to the person who made them: pick them again after the import.
            foreach (var node in graph.Nodes)
            {
                if (node.Config.ValueKind == JsonValueKind.Object && node.Config.TryGetProperty("connectionId", out _))
                {
                    var config = (JsonObject)JsonNode.Parse(node.Config.GetRawText())!;
                    config.Remove("connectionId");
                    node.Config = JsonDocument.Parse(config.ToJsonString()).RootElement.Clone();
                }
            }

            var name = package["name"]?.GetValue<string>();
            var created = await _authoring.CreateDraftAsync(
                _access.CurrentUserId,
                string.IsNullOrWhiteSpace(name) ? "Imported agent" : name!.Trim());
            await _authoring.UpdateGraphAsync(created.Id, graph);
            await _authoring.UpdateMetaAsync(
                created.Id,
                created.Name,
                package["description"]?.GetValue<string>(),
                package["icon"]?.GetValue<string>());
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Automation imported");
            return OkPayload(DefinitionPayload(await _authoring.GetAsync(created.Id)), nameof(Builder), new { id = created.Id });
        }
    }
}
