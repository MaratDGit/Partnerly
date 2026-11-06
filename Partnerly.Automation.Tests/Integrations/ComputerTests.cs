using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Integration;
using Partnerly.Computer;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Integrations.Common;
using Partnerly.Models;

namespace Partnerly.Automation.Tests.Integrations
{
    public sealed class FakeComputerGateway : IComputerGateway
    {
        public List<(Guid Connection, string Op, JsonObject Args)> Sent { get; } = new();
        public ComputerResponse Reply { get; set; } = new() { Ok = true, Data = new JsonObject { ["done"] = true } };
        public Exception? Throw { get; set; }

        public bool IsOnline(Guid connectionId) => Throw == null;

        public Task<ComputerResponse> SendAsync(Guid connectionId, string op, JsonObject args, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Throw != null)
            {
                throw Throw;
            }

            Sent.Add((connectionId, op, (JsonObject)args.DeepClone()));
            return Task.FromResult(Reply);
        }
    }

    // Keeps uploaded bytes so tests can see what the screenshot / file cards stored.
    public sealed class UploadingAttachmentService : IAttachmentService
    {
        public Dictionary<Guid, (Attachment Meta, byte[] Bytes)> Files { get; } = new();

        public Guid Add(string name, string text)
        {
            var id = Guid.NewGuid();
            Files[id] = (new Attachment { Id = id, FileName = name, ContentType = "text/plain", StoredFileName = name, StoragePath = name, EntityType = "test" }, Encoding.UTF8.GetBytes(text));
            return id;
        }

        public async Task<Attachment> UploadAsync(IFormFile file, string entityType, Guid entityId)
        {
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);
            var attachment = new Attachment { Id = Guid.NewGuid(), FileName = file.FileName, EntityType = entityType, EntityId = entityId, StoredFileName = file.FileName, StoragePath = file.FileName };
            Files[attachment.Id] = (attachment, buffer.ToArray());
            return attachment;
        }

        public Task<Attachment?> GetByIdAsync(Guid id) => Task.FromResult(Files.TryGetValue(id, out var item) ? item.Meta : null);
        public Task<Stream?> GetFileAsync(Guid id) => Task.FromResult<Stream?>(Files.TryGetValue(id, out var item) ? new MemoryStream(item.Bytes) : null);
        public Task<List<Attachment>> GetAsync(string entityType, Guid entityId) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
        public Task<IEnumerable<Attachment?>> GetAllAsync() => throw new NotSupportedException();
        public IQueryable<Attachment> QueryEntity(IEnumerable<string> fields) => throw new NotSupportedException();
    }

    public class ComputerActionTests
    {
        private static ServiceProvider Host(Guid owner, FakeComputerGateway gateway, IAttachmentService? attachments = null) =>
            AutomationTestHost.Create(owner, clock: new FakeClock(), configure: services =>
            {
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
                services.AddPartnerlyIntegrations(hostPolling: false);
                services.AddPartnerlyComputer();
                services.AddSingleton<IComputerGateway>(gateway);
                if (attachments != null)
                {
                    services.AddSingleton(attachments);
                }
            });

        private static async Task<(Guid Connection, Guid Agent)> PublishAsync(ServiceProvider provider, Guid owner, string type, JsonObject config)
        {
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Computer, new JsonObject { ["tokenHash"] = "x" });
            config["connectionId"] = connection.Id.ToString();
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, type, config);
            return (connection.Id, id);
        }

        [Fact]
        public async Task Notify_sends_the_command_with_filled_in_variables()
        {
            var owner = Guid.NewGuid();
            var gateway = new FakeComputerGateway();
            await using var provider = Host(owner, gateway);
            var (connection, agent) = await PublishAsync(provider, owner, ComputerTypeCodes.Notify, new JsonObject { ["title"] = "Hi", ["text"] = "Run {{execution.dryRun}}" });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, agent);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var sent = Assert.Single(gateway.Sent);
            Assert.Equal(connection, sent.Connection);
            Assert.Equal("notify", sent.Op);
            Assert.Equal("Run false", sent.Args["text"]!.ToString());
            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
        }

        [Fact]
        public async Task Offline_computer_fails_the_step_with_a_readable_reason()
        {
            var owner = Guid.NewGuid();
            var gateway = new FakeComputerGateway { Throw = new IntegrationException("The computer is offline. Start Partnerly Agent on it and try again.", retryable: false) };
            await using var provider = Host(owner, gateway);
            var (_, agent) = await PublishAsync(provider, owner, ComputerTypeCodes.Lock, new JsonObject());

            var (run, step) = await IntegrationTestHost.RunAsync(provider, agent);

            Assert.Equal(AutomationStatuses.Failed, run.Status);
            Assert.Contains("offline", step!.Error);
        }

        [Fact]
        public async Task A_refusal_of_the_agent_is_shown_and_not_retried()
        {
            var owner = Guid.NewGuid();
            var gateway = new FakeComputerGateway { Reply = new ComputerResponse { Ok = false, Error = "This place is not allowed in the Partnerly Agent settings (allowedFolders)." } };
            await using var provider = Host(owner, gateway);
            var (_, agent) = await PublishAsync(provider, owner, ComputerTypeCodes.ReadFile, new JsonObject { ["path"] = "C:/Windows/win.ini" });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, agent);

            Assert.Equal(AutomationStatuses.Failed, run.Status);
            Assert.Contains("allowedFolders", step!.Error);
            Assert.Single(gateway.Sent);
        }

        [Fact]
        public async Task Test_run_skips_changes_but_still_reads()
        {
            var owner = Guid.NewGuid();
            var gateway = new FakeComputerGateway();
            await using var provider = Host(owner, gateway);
            var (_, writeAgent) = await PublishAsync(provider, owner, ComputerTypeCodes.WriteFile, new JsonObject { ["path"] = "C:/x.txt", ["text"] = "a" });
            var (_, readAgent) = await PublishAsync(provider, owner, ComputerTypeCodes.SystemInfo, new JsonObject());

            await IntegrationTestHost.RunAsync(provider, writeAgent, dryRun: true);
            Assert.Empty(gateway.Sent);

            await IntegrationTestHost.RunAsync(provider, readAgent, dryRun: true);
            Assert.Equal("system_info", Assert.Single(gateway.Sent).Op);
        }

        [Fact]
        public async Task Another_users_computer_cannot_be_used()
        {
            var owner = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var gateway = new FakeComputerGateway();
            await using var provider = Host(owner, gateway);
            var theirs = await IntegrationTestHost.ConnectAsync(provider, stranger, IntegrationKinds.Computer, new JsonObject { ["tokenHash"] = "x" });
            var agent = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, ComputerTypeCodes.Lock, new JsonObject { ["connectionId"] = theirs.Id.ToString() });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, agent);

            Assert.Equal(AutomationStatuses.Failed, run.Status);
            Assert.Contains("not found", step!.Error);
            Assert.Empty(gateway.Sent);
        }

        [Fact]
        public async Task Screenshot_is_stored_as_an_attachment_and_the_bytes_leave_the_run()
        {
            var owner = Guid.NewGuid();
            var png = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
            var gateway = new FakeComputerGateway { Reply = new ComputerResponse { Ok = true, Data = new JsonObject { ["fileName"] = "shot.png", ["contentBase64"] = png } } };
            var attachments = new UploadingAttachmentService();
            await using var provider = Host(owner, gateway, attachments);
            var (_, agent) = await PublishAsync(provider, owner, ComputerTypeCodes.Screenshot, new JsonObject());

            var (run, step) = await IntegrationTestHost.RunAsync(provider, agent);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var output = JsonNode.Parse(step!.OutputJson!)!;
            var attachmentId = Guid.Parse(output["attachmentId"]!.ToString());
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, attachments.Files[attachmentId].Bytes);
            Assert.Equal("shot.png", attachments.Files[attachmentId].Meta.FileName);
            Assert.Null(output["contentBase64"]);
        }

        [Fact]
        public async Task Save_attachment_sends_the_file_to_the_computer()
        {
            var owner = Guid.NewGuid();
            var gateway = new FakeComputerGateway();
            var attachments = new UploadingAttachmentService();
            var file = attachments.Add("report.txt", "hello");
            await using var provider = Host(owner, gateway, attachments);
            var (_, agent) = await PublishAsync(provider, owner, ComputerTypeCodes.SaveAttachment, new JsonObject { ["attachmentId"] = file.ToString(), ["folder"] = "C:/Docs" });

            await IntegrationTestHost.RunAsync(provider, agent);

            var sent = Assert.Single(gateway.Sent);
            Assert.Equal("save_file", sent.Op);
            Assert.Equal("report.txt", sent.Args["fileName"]!.ToString());
            Assert.Equal("hello", Encoding.UTF8.GetString(Convert.FromBase64String(sent.Args["contentBase64"]!.ToString())));
            Assert.Null(sent.Args["attachmentId"]);
        }

        [Fact]
        public async Task Every_computer_card_is_registered_for_the_computer_connection()
        {
            await using var provider = Host(Guid.NewGuid(), new FakeComputerGateway());
            var actions = provider.GetRequiredService<Partnerly.Automation.Registry.ActionRegistry>().DescribeAll(provider).Where(item => item.Type.StartsWith("computer.")).ToList();
            var triggers = provider.GetRequiredService<Partnerly.Automation.Registry.TriggerRegistry>().DescribeAll(provider).Where(item => item.Type.StartsWith("computer.")).ToList();

            Assert.Equal(36, actions.Count);
            Assert.Equal(12, triggers.Count);
            Assert.All(actions.Concat(triggers), item => Assert.Equal(IntegrationKinds.Computer, item.ConnectionKind));
        }

        [Fact]
        public async Task File_event_starts_only_agents_whose_folder_and_pattern_match()
        {
            var owner = Guid.NewGuid();
            await using var provider = Host(owner, new FakeComputerGateway());
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Computer, new JsonObject { ["tokenHash"] = "x" });
            var pdfAgent = await IntegrationTestHost.PublishTriggerAsync(provider, owner, ComputerTypeCodes.FileCreated,
                new JsonObject { ["connectionId"] = connection.Id.ToString(), ["folder"] = "C:/Inbox", ["pattern"] = "*.pdf" });
            var anyAgent = await IntegrationTestHost.PublishTriggerAsync(provider, owner, ComputerTypeCodes.FileCreated,
                new JsonObject { ["connectionId"] = connection.Id.ToString(), ["folder"] = "D:/Other" });

            var payload = new JsonObject { ["folder"] = "C:\\Inbox", ["fileName"] = "Invoice.PDF" };
            var dispatcher = provider.GetRequiredService<AutomationTriggerDispatcher>();
            await dispatcher.EnqueueIntegrationAsync(ComputerTypeCodes.FileCreated, owner, connection.Id, payload, config => ComputerEvents.MatchesFile(config, payload));

            Assert.Single(IntegrationTestHost.RunsOf(provider, pdfAgent));
            Assert.Empty(IntegrationTestHost.RunsOf(provider, anyAgent));
        }
    }

    public class ComputerEventFilterTests
    {
        private static JsonElement Config(string json) => JsonDocument.Parse(json).RootElement.Clone();

        [Theory]
        [InlineData("*.pdf", "a.PDF", true)]
        [InlineData("*.pdf;*.docx", "b.docx", true)]
        [InlineData("report-?.txt", "report-1.txt", true)]
        [InlineData("report-?.txt", "report-12.txt", false)]
        [InlineData("*.pdf", "a.pdf.exe", false)]
        public void Wildcards_match_like_file_masks(string pattern, string name, bool expected) =>
            Assert.Equal(expected, ComputerEvents.Wildcard(pattern, name));

        [Fact]
        public void Folder_filter_includes_subfolders_but_not_look_alike_names()
        {
            var config = Config("{\"folder\":\"C:/Inbox\"}".Replace('/', '\\').Replace("\\", "\\\\"));
            Assert.True(ComputerEvents.MatchesFile(config, new JsonObject { ["folder"] = "C:\\Inbox", ["fileName"] = "a" }));
            Assert.True(ComputerEvents.MatchesFile(config, new JsonObject { ["folder"] = "C:\\Inbox\\Sub", ["fileName"] = "a" }));
            Assert.False(ComputerEvents.MatchesFile(config, new JsonObject { ["folder"] = "C:\\InboxEvil", ["fileName"] = "a" }));
        }

        [Fact]
        public void Status_and_program_filters()
        {
            Assert.True(ComputerEvents.MatchesStatus(Config("{}"), "online"));
            Assert.True(ComputerEvents.MatchesStatus(Config("{\"state\":\"offline\"}"), "offline"));
            Assert.False(ComputerEvents.MatchesStatus(Config("{\"state\":\"offline\"}"), "online"));
            Assert.True(ComputerEvents.MatchesProgram(Config("{\"program\":\"Notepad.exe\"}"), new JsonObject { ["name"] = "notepad" }));
            Assert.False(ComputerEvents.MatchesProgram(Config("{\"program\":\"chrome\"}"), new JsonObject { ["name"] = "notepad" }));
        }
    }

    public class ComputerTokenTests
    {
        [Fact]
        public void Token_names_its_connection_and_only_the_hash_is_kept()
        {
            var id = Guid.NewGuid();
            var (token, hash) = ComputerToken.Create(id);

            Assert.True(ComputerToken.TryParse(token, out var parsedId, out var secret));
            Assert.Equal(id, parsedId);
            Assert.True(ComputerToken.Matches(secret, hash));
            Assert.DoesNotContain(secret, hash);
            Assert.False(ComputerToken.Matches(secret + "0", hash));
            Assert.False(ComputerToken.Matches(secret, null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("pc.123.abc")]
        [InlineData("xx.00000000000000000000000000000000.0123456789abcdef0123")]
        public void Broken_tokens_are_refused(string token) =>
            Assert.False(ComputerToken.TryParse(token, out _, out _));
    }

    public class ComputerGatewayTests
    {
        private sealed class FakeProxy : IClientProxy
        {
            private readonly Func<string, object?[], Task> _onSend;
            public FakeProxy(Func<string, object?[], Task> onSend) { _onSend = onSend; }
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => _onSend(method, args);
        }

        private sealed class FakeClients : IHubClients
        {
            private readonly IClientProxy _proxy;
            public FakeClients(IClientProxy proxy) { _proxy = proxy; }
            public IClientProxy All => _proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Client(string connectionId) => _proxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
            public IClientProxy Group(string groupName) => _proxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
            public IClientProxy User(string userId) => _proxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
        }

        private sealed class FakeHub : IHubContext<ComputerHub>
        {
            public FakeHub(IClientProxy proxy) { Clients = new FakeClients(proxy); }
            public IHubClients Clients { get; }
            public IGroupManager Groups => throw new NotSupportedException();
        }

        [Fact]
        public async Task Command_waits_for_the_answer_of_the_same_computer()
        {
            var computer = Guid.NewGuid();
            ComputerGateway? gateway = null;
            var hub = new FakeHub(new FakeProxy((method, args) =>
            {
                if (method == "Execute")
                {
                    gateway!.Complete(computer, (string)args[0]!, new ComputerResponse { Ok = true, Data = new JsonObject { ["op"] = (string)args[1]! } });
                }

                return Task.CompletedTask;
            }));
            gateway = new ComputerGateway(hub);
            await gateway.AttachAsync(computer, "signalr-1");

            var answer = await gateway.SendAsync(computer, "ping", new JsonObject(), TimeSpan.FromSeconds(5), CancellationToken.None);

            Assert.True(gateway.IsOnline(computer));
            Assert.True(answer.Ok);
            Assert.Equal("ping", answer.Data["op"]!.ToString());
        }

        [Fact]
        public async Task Another_computer_cannot_answer_a_command()
        {
            var computer = Guid.NewGuid();
            var other = Guid.NewGuid();
            ComputerGateway? gateway = null;
            var accepted = true;
            var hub = new FakeHub(new FakeProxy((method, args) =>
            {
                if (method == "Execute")
                {
                    accepted = gateway!.Complete(other, (string)args[0]!, new ComputerResponse { Ok = true });
                }

                return Task.CompletedTask;
            }));
            gateway = new ComputerGateway(hub);
            await gateway.AttachAsync(computer, "signalr-1");

            await Assert.ThrowsAnyAsync<IntegrationException>(() => gateway.SendAsync(computer, "ping", new JsonObject(), TimeSpan.FromMilliseconds(200), CancellationToken.None));
            Assert.False(accepted);
        }

        [Fact]
        public async Task Offline_computer_is_reported_before_anything_is_sent()
        {
            var gateway = new ComputerGateway(new FakeHub(new FakeProxy((_, _) => Task.CompletedTask)));

            var error = await Assert.ThrowsAnyAsync<IntegrationException>(() => gateway.SendAsync(Guid.NewGuid(), "ping", new JsonObject(), TimeSpan.FromSeconds(1), CancellationToken.None));

            Assert.Contains("offline", error.Message);
        }

        [Fact]
        public async Task A_replaced_connection_cannot_take_the_computer_offline()
        {
            var computer = Guid.NewGuid();
            var gateway = new ComputerGateway(new FakeHub(new FakeProxy((_, _) => Task.CompletedTask)));
            await gateway.AttachAsync(computer, "old");
            await gateway.AttachAsync(computer, "new");

            Assert.False(gateway.Detach(computer, "old"));
            Assert.True(gateway.IsOnline(computer));
            Assert.True(gateway.Detach(computer, "new"));
            Assert.False(gateway.IsOnline(computer));
        }

        [Fact]
        public void A_flood_of_events_is_cut_off()
        {
            var gateway = new ComputerGateway(new FakeHub(new FakeProxy((_, _) => Task.CompletedTask)));
            var computer = Guid.NewGuid();

            var allowed = Enumerable.Range(0, 200).Count(_ => gateway.AllowEvent(computer));

            Assert.Equal(120, allowed);
            Assert.False(gateway.AllowEvent(Guid.Empty));
        }
    }
}
