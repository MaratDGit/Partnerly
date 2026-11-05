using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Integrations.Common;
using Partnerly.Models;

namespace Partnerly.Automation.Tests.Integrations
{
    public sealed class RecordedRequest
    {
        public HttpMethod Method { get; init; } = HttpMethod.Get;
        public string Url { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string? Authorization { get; init; }
        public string? ContentType { get; init; }
    }

    // Answers by the first route whose predicate matches; records every call.
    public sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly List<(Func<HttpRequestMessage, bool> match, Func<RecordedRequest, (HttpStatusCode, string)> reply)> _routes = new();

        public List<RecordedRequest> Requests { get; } = new();

        public FakeHttpHandler On(string urlPart, string json, HttpStatusCode status = HttpStatusCode.OK, HttpMethod? method = null)
        {
            _routes.Add((req => req.RequestUri!.ToString().Contains(urlPart, StringComparison.Ordinal) && (method == null || req.Method == method),
                _ => (status, json)));
            return this;
        }

        public FakeHttpHandler On(string urlPart, Func<RecordedRequest, (HttpStatusCode, string)> reply)
        {
            _routes.Add((req => req.RequestUri!.ToString().Contains(urlPart, StringComparison.Ordinal), reply));
            return this;
        }

        public IEnumerable<RecordedRequest> To(string urlPart) =>
            Requests.Where(item => item.Url.Contains(urlPart, StringComparison.Ordinal));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var recorded = new RecordedRequest
            {
                Method = request.Method,
                Url = request.RequestUri!.ToString(),
                Body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken),
                Authorization = request.Headers.Authorization?.ToString(),
                ContentType = request.Content?.Headers.ContentType?.ToString()
            };
            Requests.Add(recorded);
            foreach (var (match, reply) in _routes)
            {
                if (match(request))
                {
                    var (status, json) = reply(recorded);
                    return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"no fake route\"}") };
        }

        // HttpClientFactory recycles handlers; keep the fake alive for the test.
        protected override void Dispose(bool disposing)
        {
        }
    }

    public sealed class FakeAttachmentService : IAttachmentService
    {
        public Dictionary<Guid, (Attachment meta, byte[] bytes)> Files { get; } = new();

        public Guid Add(string name, string contentType, string text)
        {
            var id = Guid.NewGuid();
            Files[id] = (new Attachment
            {
                Id = id,
                FileName = name,
                ContentType = contentType,
                StoredFileName = name,
                StoragePath = name,
                EntityType = "test"
            }, Encoding.UTF8.GetBytes(text));
            return id;
        }

        public Task<Attachment?> GetByIdAsync(Guid id) =>
            Task.FromResult(Files.TryGetValue(id, out var item) ? item.meta : null);

        public Task<Stream?> GetFileAsync(Guid id) =>
            Task.FromResult<Stream?>(Files.TryGetValue(id, out var item) ? new MemoryStream(item.bytes) : null);

        public Task<Attachment> UploadAsync(IFormFile file, string entityType, Guid entityId) => throw new NotSupportedException();
        public Task<List<Attachment>> GetAsync(string entityType, Guid entityId) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
        public Task<IEnumerable<Attachment?>> GetAllAsync() => throw new NotSupportedException();
        public IQueryable<Attachment> QueryEntity(IEnumerable<string> fields) => throw new NotSupportedException();
    }

    public static class IntegrationTestHost
    {
        public static ServiceProvider Create(
            Guid owner,
            FakeHttpHandler http,
            FakeClock? clock = null,
            IAttachmentService? attachments = null)
        {
            return AutomationTestHost.Create(owner, clock: clock ?? new FakeClock(), configure: services =>
            {
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Google:ClientId"] = "test-client",
                        ["Authentication:Google:ClientSecret"] = "test-secret",
                        ["Authentication:Microsoft:ClientId"] = "ms-client",
                        ["Authentication:Microsoft:ClientSecret"] = "ms-secret"
                    })
                    .Build());
                services.AddPartnerlyIntegrations(hostPolling: false);
                services.AddHttpClient(IntegrationHttp.ClientName).ConfigurePrimaryHttpMessageHandler(() => http);
                if (attachments != null)
                {
                    services.AddSingleton(attachments);
                }
            });
        }

        public static async Task<IntegrationConnection> ConnectAsync(IServiceProvider provider, Guid owner, string kind, JsonObject payload)
        {
            using var scope = provider.CreateScope();
            var connections = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
            return await connections.CreateAsync(owner, kind, kind + " test", payload);
        }

        public static async Task<IntegrationConnection> ReloadAsync(IServiceProvider provider, Guid id)
        {
            using var scope = provider.CreateScope();
            return (await scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>().GetAsync(id))!;
        }

        // Manual trigger -> one action node named "step".
        public static async Task<Guid> PublishSingleActionAsync(IServiceProvider provider, Guid owner, string actionType, JsonObject config)
        {
            var graph = new JsonObject
            {
                ["schemaVersion"] = 1,
                ["nodes"] = new JsonArray(
                    new JsonObject { ["id"] = "t1", ["kind"] = "trigger", ["type"] = "manual", ["name"] = "start", ["config"] = new JsonObject() },
                    new JsonObject { ["id"] = "a1", ["kind"] = "action", ["type"] = actionType, ["name"] = "step", ["config"] = config }),
                ["edges"] = new JsonArray(new JsonObject { ["id"] = "e1", ["from"] = "t1", ["to"] = "a1" })
            };
            return await PublishAsync(provider, owner, graph);
        }

        public static async Task<Guid> PublishTriggerAsync(IServiceProvider provider, Guid owner, string triggerType, JsonObject config)
        {
            var graph = new JsonObject
            {
                ["schemaVersion"] = 1,
                ["nodes"] = new JsonArray(
                    new JsonObject { ["id"] = "t1", ["kind"] = "trigger", ["type"] = triggerType, ["name"] = "incoming", ["config"] = config },
                    new JsonObject { ["id"] = "a1", ["kind"] = "action", ["type"] = "set_variable", ["name"] = "mark", ["config"] = new JsonObject { ["name"] = "seen", ["value"] = "1" } }),
                ["edges"] = new JsonArray(new JsonObject { ["id"] = "e1", ["from"] = "t1", ["to"] = "a1" })
            };
            return await PublishAsync(provider, owner, graph);
        }

        public static async Task<(AutomationRun run, AutomationRunStep? step)> RunAsync(IServiceProvider provider, Guid automationId, bool dryRun = false)
        {
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var queued = await authoring.EnqueueRunAsync(automationId, "test", dryRun);
            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            var steps = await store.GetStepsAsync(run.Id);
            return (run, steps.LastOrDefault(item => item.NodeId == "a1"));
        }

        public static List<AutomationRun> RunsOf(IServiceProvider provider, Guid automationId) =>
            provider.GetRequiredService<IAutomationStore>().QueryRuns().Where(item => item.AutomationId == automationId).ToList();

        public static JsonObject TriggerPayload(AutomationRun run) =>
            JsonNode.Parse(run.ContextJson)?["trigger"] as JsonObject ?? new JsonObject();

        public static async Task<int> PollAsync(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IntegrationPollingService>().PollOnceAsync();
        }

        private static async Task<Guid> PublishAsync(IServiceProvider provider, Guid owner, JsonObject graph)
        {
            // The test host acts as one user; an agent of another user is
            // published as that user and then handed over in the store.
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var hostUser = provider.GetRequiredService<IAutomationAccessService>().CurrentUserId;
            var automation = await authoring.CreateDraftAsync(hostUser, "integration");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(graph.ToJsonString()));
            await authoring.PublishAsync(automation.Id);
            if (owner != hostUser)
            {
                var store = provider.GetRequiredService<IAutomationStore>();
                var definition = (await store.GetDefinitionAsync(automation.Id))!;
                definition.OwnerUserId = owner;
                await store.UpdateDefinitionAsync(definition);
            }

            return automation.Id;
        }
    }
}
