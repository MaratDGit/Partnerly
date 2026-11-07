using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Computer;
using Partnerly.Integrations.Common;

namespace Partnerly.Automation.Tests.Integrations
{
    public class ComputerGitCardTests
    {
        private static ServiceProvider Host(Guid owner, FakeComputerGateway gateway) =>
            AutomationTestHost.Create(owner, clock: new FakeClock(), configure: services =>
            {
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
                services.AddPartnerlyIntegrations(hostPolling: false);
                services.AddPartnerlyComputer();
                services.AddSingleton<IComputerGateway>(gateway);
            });

        private static async Task<(AutomationRunResult Run, JsonObject? Sent)> RunCardAsync(string type, JsonObject config, FakeComputerGateway gateway)
        {
            var owner = Guid.NewGuid();
            await using var provider = Host(owner, gateway);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Computer, new JsonObject { ["tokenHash"] = "x" });
            config["connectionId"] = connection.Id.ToString();
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, type, config);
            var (run, step) = await IntegrationTestHost.RunAsync(provider, id);
            return (new AutomationRunResult(run.Status, step?.Status, step?.Error, step?.OutputJson), gateway.Sent.LastOrDefault().Args);
        }

        public sealed record AutomationRunResult(string RunStatus, string? StepStatus, string? Error, string? Output);

        private static FakeComputerGateway Reply(JsonObject data) =>
            new() { Reply = new ComputerResponse { Ok = true, Data = data } };

        [Fact]
        public async Task Status_card_sends_a_git_command_and_shapes_the_answer()
        {
            var gateway = Reply(new JsonObject
            {
                ["output"] = "## main...origin/main [ahead 2, behind 1]\n M a.txt\n?? b.txt\nA  c.txt\n"
            });

            var (result, sent) = await RunCardAsync(ComputerTypeCodes.GitStatus, new JsonObject { ["repo"] = "C:/Dev/app" }, gateway);

            Assert.Equal(AutomationStatuses.StepSucceeded, result.StepStatus);
            Assert.Equal("git", gateway.Sent.Single().Op);
            Assert.Equal("status", sent!["sub"]!.ToString());
            Assert.Equal("C:/Dev/app", sent["repo"]!.ToString());
            var output = JsonNode.Parse(result.Output!)!;
            Assert.Equal("main", output["branch"]!.ToString());
            Assert.Equal("origin/main", output["upstream"]!.ToString());
            Assert.Equal(2, output["ahead"]!.GetValue<int>());
            Assert.Equal(1, output["behind"]!.GetValue<int>());
            Assert.Equal(3, output["changes"]!.GetValue<int>());
            Assert.Equal(1, output["untracked"]!.GetValue<int>());
            Assert.False(output["clean"]!.GetValue<bool>());
        }

        [Fact]
        public void Clean_status_is_clean()
        {
            var data = new JsonObject { ["output"] = "## main\n" };

            GitOutput.ShapeStatus(data);

            Assert.True(data["clean"]!.GetValue<bool>());
            Assert.Equal("main: clean", data["summary"]!.ToString());
        }

        [Fact]
        public void Log_is_split_into_commits()
        {
            var data = new JsonObject { ["output"] = "aaa111bbb222ccc\u001fAnna\u001f2026-11-07T10:00:00+04:00\u001fFix login\nddd\u001fBob\u001f2026-11-06T09:00:00+04:00\u001fAdd tests" };

            GitOutput.ShapeLog(data);

            Assert.Equal(2, data["count"]!.GetValue<int>());
            Assert.Equal("Fix login", data["first"]!["subject"]!.ToString());
            Assert.Equal("aaa111b", data["first"]!["shortSha"]!.ToString());
        }

        [Fact]
        public void Branches_report_the_current_one()
        {
            var data = new JsonObject { ["output"] = " |dev\n*|main\n |origin/main\n" };

            GitOutput.ShapeBranches(data);

            Assert.Equal("main", data["current"]!.ToString());
            Assert.Equal("dev, main, origin/main", data["names"]!.ToString());
        }

        [Theory]
        [InlineData("log --oneline -5", new[] { "log", "--oneline", "-5" })]
        [InlineData("git commit -m \"two words\"", new[] { "git", "commit", "-m", "two words" })]
        [InlineData("tag -a v1 -m 'it works'", new[] { "tag", "-a", "v1", "-m", "it works" })]
        [InlineData("   status   ", new[] { "status" })]
        [InlineData("commit -m \"\"", new[] { "commit", "-m", "" })]
        public void Command_text_is_split_like_a_terminal_would(string text, string[] expected)
        {
            Assert.Equal(expected, GitOutput.Tokenize(text));
        }

        [Fact]
        public async Task Commit_card_sends_message_and_what_to_add()
        {
            var gateway = Reply(new JsonObject { ["committed"] = true, ["sha"] = "abc" });

            var (result, sent) = await RunCardAsync(ComputerTypeCodes.GitCommit,
                new JsonObject { ["repo"] = "C:/Dev/app", ["message"] = "Daily backup", ["add"] = "all" }, gateway);

            Assert.Equal(AutomationStatuses.StepSucceeded, result.StepStatus);
            Assert.Equal("commit", sent!["sub"]!.ToString());
            Assert.Equal("all", sent["add"]!.ToString());
            Assert.Equal(new[] { "-m", "Daily backup" }, sent["args"]!.AsArray().Select(item => item!.ToString()).ToArray());
        }

        [Fact]
        public async Task A_value_that_looks_like_an_option_never_reaches_the_pc()
        {
            var gateway = Reply(new JsonObject());

            var (result, _) = await RunCardAsync(ComputerTypeCodes.GitPush,
                new JsonObject { ["repo"] = "C:/Dev/app", ["remote"] = "origin", ["branch"] = "--force" }, gateway);

            Assert.Equal(AutomationStatuses.StepFailed, result.StepStatus);
            Assert.Contains("dash", result.Error);
            Assert.Empty(gateway.Sent);
        }

        [Fact]
        public async Task Push_card_builds_a_normal_push_and_a_force_option_only_when_chosen()
        {
            var gateway = Reply(new JsonObject());

            var (_, plain) = await RunCardAsync(ComputerTypeCodes.GitPush,
                new JsonObject { ["repo"] = "C:/Dev/app", ["branch"] = "main", ["setUpstream"] = "true" }, gateway);
            var (_, forced) = await RunCardAsync(ComputerTypeCodes.GitPush,
                new JsonObject { ["repo"] = "C:/Dev/app", ["force"] = "lease" }, Reply(new JsonObject()));

            Assert.Equal(new[] { "-u", "origin", "main" }, plain!["args"]!.AsArray().Select(item => item!.ToString()).ToArray());
            Assert.Equal(new[] { "--force-with-lease" }, forced!["args"]!.AsArray().Select(item => item!.ToString()).ToArray());
        }

        [Fact]
        public async Task Generic_command_card_splits_the_text_into_sub_command_and_arguments()
        {
            var gateway = Reply(new JsonObject { ["output"] = "ok" });

            var (_, sent) = await RunCardAsync(ComputerTypeCodes.GitCommand,
                new JsonObject { ["repo"] = "C:/Dev/app", ["command"] = "git cherry-pick abc123" }, gateway);

            Assert.Equal("cherry-pick", sent!["sub"]!.ToString());
            Assert.Equal(new[] { "abc123" }, sent["args"]!.AsArray().Select(item => item!.ToString()).ToArray());
        }

        [Fact]
        public async Task Test_run_skips_changing_git_cards_but_reads_status()
        {
            var owner = Guid.NewGuid();
            var gateway = Reply(new JsonObject { ["output"] = "## main\n" });
            await using var provider = Host(owner, gateway);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Computer, new JsonObject { ["tokenHash"] = "x" });
            var push = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, ComputerTypeCodes.GitPush,
                new JsonObject { ["connectionId"] = connection.Id.ToString(), ["repo"] = "C:/Dev/app" });
            var status = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, ComputerTypeCodes.GitStatus,
                new JsonObject { ["connectionId"] = connection.Id.ToString(), ["repo"] = "C:/Dev/app" });

            var (_, pushStep) = await IntegrationTestHost.RunAsync(provider, push, dryRun: true);
            var (_, statusStep) = await IntegrationTestHost.RunAsync(provider, status, dryRun: true);

            Assert.Equal(AutomationStatuses.StepSkipped, pushStep!.Status);
            Assert.Equal(AutomationStatuses.StepSucceeded, statusStep!.Status);
            Assert.Single(gateway.Sent);
        }

        [Fact]
        public void Git_trigger_filters_match_repo_branch_and_state()
        {
            var payload = new JsonObject { ["repo"] = "C:\\Dev\\app", ["branch"] = "main", ["state"] = "dirty" };
            JsonElement Config(string json) => JsonDocument.Parse(json).RootElement.Clone();

            Assert.True(ComputerEvents.MatchesRepo(Config("{}"), payload, checkState: true));
            Assert.True(ComputerEvents.MatchesRepo(Config("{\"repo\":\"c:/dev/app\"}"), payload, checkState: true));
            Assert.False(ComputerEvents.MatchesRepo(Config("{\"repo\":\"C:/Other\"}"), payload, checkState: true));
            Assert.False(ComputerEvents.MatchesRepo(Config("{\"branch\":\"dev\"}"), payload, checkState: false));
            Assert.True(ComputerEvents.MatchesRepo(Config("{\"state\":\"dirty\"}"), payload, checkState: true));
            Assert.False(ComputerEvents.MatchesRepo(Config("{\"state\":\"clean\"}"), payload, checkState: true));
        }
    }
}
