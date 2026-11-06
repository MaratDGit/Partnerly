using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Agent.Config;
using Partnerly.Automation.Abstractions;
using Partnerly.Computer;
using Partnerly.Integrations.Common;

namespace Partnerly.Automation.Tests.Integrations
{
    // The second wave of Computer cards: sound, power, downloads, USB, idle, disk...
    public class ComputerMoreCardTests
    {
        private static ServiceProvider Host(Guid owner, FakeComputerGateway gateway) =>
            AutomationTestHost.Create(owner, clock: new FakeClock(), configure: services =>
            {
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
                services.AddPartnerlyIntegrations(hostPolling: false);
                services.AddPartnerlyComputer();
                services.AddSingleton<IComputerGateway>(gateway);
            });

        // Every new card sends exactly the command the agent understands, with the card's fields.
        [Theory]
        [InlineData(ComputerTypeCodes.CreateFolder, "create_folder", "path", "C:/Docs/New")]
        [InlineData(ComputerTypeCodes.DeleteFile, "delete_file", "path", "C:/Docs/old.txt")]
        [InlineData(ComputerTypeCodes.FileInfo, "file_info", "path", "C:/Docs/a.txt")]
        [InlineData(ComputerTypeCodes.Unzip, "unzip", "destination", "C:/Docs/out")]
        [InlineData(ComputerTypeCodes.Volume, "volume", "action", "down")]
        [InlineData(ComputerTypeCodes.Media, "media", "action", "next")]
        [InlineData(ComputerTypeCodes.Shutdown, "shutdown", "mode", "restart")]
        [InlineData(ComputerTypeCodes.PingHost, "ping_host", "host", "example.com")]
        [InlineData(ComputerTypeCodes.DownloadUrl, "download_url", "url", "https://example.com/a.pdf")]
        public async Task New_cards_send_their_command(string type, string op, string field, string value)
        {
            var owner = Guid.NewGuid();
            var gateway = new FakeComputerGateway();
            await using var provider = Host(owner, gateway);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Computer, new JsonObject { ["tokenHash"] = "x" });
            var config = new JsonObject { ["connectionId"] = connection.Id.ToString(), [field] = value };
            var agent = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, type, config);

            var (run, _) = await IntegrationTestHost.RunAsync(provider, agent);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var sent = Assert.Single(gateway.Sent);
            Assert.Equal(op, sent.Op);
            Assert.Equal(value, sent.Args[field]!.ToString());
        }

        [Fact]
        public void Every_new_type_code_is_unique()
        {
            var codes = typeof(ComputerTypeCodes).GetFields().Select(f => (string)f.GetRawConstantValue()!).ToList();
            Assert.Equal(codes.Count, codes.Distinct().Count());
            Assert.All(codes, code => Assert.StartsWith("computer.", code));
        }
    }

    public class ComputerMoreFilterTests
    {
        private static JsonElement Config(string json) => JsonDocument.Parse(json).RootElement.Clone();

        [Theory]
        [InlineData("{}", "connected", "E:\\", true)]
        [InlineData("{\"state\":\"removed\"}", "connected", "E:\\", false)]
        [InlineData("{\"state\":\"connected\",\"drive\":\"e\"}", "connected", "E:\\", true)]
        [InlineData("{\"drive\":\"E:\"}", "connected", "F:\\", false)]
        [InlineData("{\"state\":\"any\",\"drive\":\"E\"}", "removed", "E:\\", true)]
        public void Drive_filter_checks_state_and_letter(string config, string state, string drive, bool expected) =>
            Assert.Equal(expected, ComputerEvents.MatchesDrive(Config(config), new JsonObject { ["state"] = state, ["drive"] = drive }, checkState: true));

        [Fact]
        public void Disk_filter_ignores_the_state_and_checks_only_the_letter()
        {
            var payload = new JsonObject { ["drive"] = "C:\\", ["freeGb"] = 2 };
            Assert.True(ComputerEvents.MatchesDrive(Config("{\"state\":\"removed\",\"drive\":\"c\"}"), payload, checkState: false));
            Assert.False(ComputerEvents.MatchesDrive(Config("{\"drive\":\"D\"}"), payload, checkState: false));
        }

        [Fact]
        public void Clipboard_filter_looks_for_a_part_of_the_text_without_case()
        {
            var payload = new JsonObject { ["text"] = "Invoice 2026-001 from ACME" };
            Assert.True(ComputerEvents.MatchesClipboard(Config("{}"), payload));
            Assert.True(ComputerEvents.MatchesClipboard(Config("{\"textContains\":\"invoice\"}"), payload));
            Assert.False(ComputerEvents.MatchesClipboard(Config("{\"textContains\":\"receipt\"}"), payload));
        }

        [Theory]
        [InlineData("locked", "locked", true)]
        [InlineData("locked", "unlocked", false)]
        [InlineData("any", "woke_up", true)]
        [InlineData("battery_low", "battery_low", true)]
        [InlineData("idle", "active", false)]
        public void Event_states_use_the_same_filter(string wanted, string actual, bool expected) =>
            Assert.Equal(expected, ComputerEvents.MatchesStatus(Config("{\"state\":\"" + wanted + "\"}"), actual));
    }

    // The settings file of the program on the PC: a damaged file must never leave the person without a program.
    [Collection("AgentHome")]
    public class AgentConfigTests : IDisposable
    {
        private readonly string _home = Path.Combine(Path.GetTempPath(), "agentcfg-" + Guid.NewGuid().ToString("N"));
        private readonly string? _old = Environment.GetEnvironmentVariable("PARTNERLY_AGENT_HOME");

        public AgentConfigTests()
        {
            Directory.CreateDirectory(_home);
            Environment.SetEnvironmentVariable("PARTNERLY_AGENT_HOME", _home);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PARTNERLY_AGENT_HOME", _old);
            try { Directory.Delete(_home, true); } catch (IOException) { }
        }

        private static AgentConfig Sample() => new() { Server = "https://site.example", Token = "pc.abc.secret", AllowedFolders = { "C:/Docs" } };

        [Fact]
        public void A_saved_file_is_read_back_and_a_backup_appears_on_the_second_save()
        {
            var config = Sample();
            config.Save();
            Assert.False(File.Exists(AgentConfig.FilePath + ".bak"));
            config.Welcomed = true;
            config.Save();

            Assert.True(File.Exists(AgentConfig.FilePath + ".bak"));
            var loaded = AgentConfig.Load()!;
            Assert.Equal("https://site.example", loaded.Server);
            Assert.True(loaded.Welcomed);
            Assert.True(loaded.Enabled);
            Assert.Null(AgentConfig.LoadProblem);
        }

        [Fact]
        public void A_damaged_file_is_restored_from_the_last_good_copy()
        {
            var config = Sample();
            config.Save();
            config.Save();
            File.WriteAllText(AgentConfig.FilePath, "{ this is not json");

            var loaded = AgentConfig.Load();

            Assert.NotNull(loaded);
            Assert.Equal("pc.abc.secret", loaded!.Token);
            Assert.Equal("restored", AgentConfig.LoadProblem);
        }

        [Fact]
        public void A_damaged_file_without_a_copy_is_reported_not_hidden()
        {
            Directory.CreateDirectory(_home);
            File.WriteAllText(AgentConfig.FilePath, "{ nope");

            Assert.Null(AgentConfig.Load());
            Assert.NotNull(AgentConfig.LoadProblem);
        }

        [Fact]
        public void A_missing_file_is_simply_not_set_up_yet()
        {
            Assert.Null(AgentConfig.Load());
            Assert.Null(AgentConfig.LoadProblem);
        }

        [Fact]
        public void The_site_gets_the_lists_but_never_the_token()
        {
            var json = Sample().ToSiteJson();
            Assert.DoesNotContain("secret", json);
            Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("C:/Docs", json);
        }

        [Fact]
        public void Dangerous_commands_ask_the_owner_first_by_default()
        {
            var config = Sample();
            Assert.Contains("shutdown", config.ConfirmOps);
            Assert.Contains("delete_file", config.ConfirmOps);
        }
    }
}

namespace Partnerly.Automation.Tests.Integrations
{
    public class LocalizedErrorTests
    {
        [Fact]
        public void The_viewers_language_is_used_and_English_is_the_fallback()
        {
            var text = "Step A: " + Partnerly.Automation.Abstractions.LocalizedError.Mark("ComputerErrOffline", "The computer is offline.");

            Assert.Equal("Step A: Компьютер не в сети.", Partnerly.Automation.Abstractions.LocalizedError.Resolve(text, _ => "Компьютер не в сети."));
            Assert.Equal("Step A: The computer is offline.", Partnerly.Automation.Abstractions.LocalizedError.Resolve(text, _ => null));
            Assert.Equal("Step A: The computer is offline.", Partnerly.Automation.Abstractions.LocalizedError.Strip(text));
            Assert.Equal("plain", Partnerly.Automation.Abstractions.LocalizedError.Resolve("plain", _ => "x"));
        }
    }
}
