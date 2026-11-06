using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Tests
{
    // For each, error paths, "only continue if", value functions, agent constants, rerun / resume.
    public class EngineExtrasTests
    {
        private const string Trigger = @"{ ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} }";

        private static string SetVar(string id, string name, string value) =>
            @"{ ""id"": """ + id + @""", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": """ + id + @""", ""config"": { ""name"": """ + name + @""", ""value"": """ + value + @""" } }";

        private static string Failing(string id, string extra = "") =>
            @"{ ""id"": """ + id + @""", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": """ + id + @""", ""config"": { ""name"": ""bad name"", ""value"": ""x""" + extra + @" } }";

        private static string Edge(string from, string to, string? port = null) =>
            @"{ ""id"": ""e_" + from + "_" + to + "_" + port + @""", ""from"": """ + from + @""", ""to"": """ + to + @"""" +
            (port == null ? "" : @", ""port"": """ + port + @"""") + " }";

        private static async Task<(ServiceProvider Provider, Guid Id)> Build(string nodes, string edges, string? settings = null)
        {
            var owner = Guid.NewGuid();
            var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var automation = await authoring.CreateDraftAsync(owner, "extras");
            automation.RetryLimit = 1;
            await store.UpdateDefinitionAsync(automation);
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(
                @"{ ""schemaVersion"": 1, ""nodes"": [" + nodes + @"], ""edges"": [" + edges + "]" +
                (settings == null ? "" : @", ""settings"": " + settings) + " }"));
            await authoring.PublishAsync(automation.Id);
            return (provider, automation.Id);
        }

        private static async Task<AutomationRun> RunLive(ServiceProvider provider, Guid id)
        {
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var queued = await authoring.EnqueueRunAsync(id, "test", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            return (await store.GetRunAsync(queued.Id))!;
        }

        private static JsonObject Vars(AutomationRun run) =>
            (JsonNode.Parse(run.ContextJson)!.AsObject()["vars"] as JsonObject)!;

        [Fact]
        public async Task For_each_repeats_its_cards_for_every_item_and_then_opens_done()
        {
            var loop = @"{ ""id"": ""l1"", ""kind"": ""action"", ""type"": ""loop"", ""name"": ""each"", ""config"": { ""items"": ""[\""a\"",\""b\"",\""c\""]"" } }";
            var (provider, id) = await Build(
                Trigger + "," + loop + "," + SetVar("s1", "log", "{{vars.log}}{{loop.item}}{{loop.number}};") + "," + SetVar("s2", "after", "{{vars.log}}"),
                Edge("t1", "l1") + "," + Edge("l1", "s1", "each") + "," + Edge("l1", "s2", "done"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Succeeded, run.Status);
                Assert.Equal("a1;b2;c3;", Vars(run)["log"]!.GetValue<string>());
                Assert.Equal("a1;b2;c3;", Vars(run)["after"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task For_each_over_an_empty_list_goes_straight_to_done()
        {
            var loop = @"{ ""id"": ""l1"", ""kind"": ""action"", ""type"": ""loop"", ""name"": ""each"", ""config"": { ""items"": ""[]"" } }";
            var (provider, id) = await Build(
                Trigger + "," + loop + "," + SetVar("s1", "body", "yes") + "," + SetVar("s2", "after", "yes"),
                Edge("t1", "l1") + "," + Edge("l1", "s1", "each") + "," + Edge("l1", "s2", "done"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Succeeded, run.Status);
                Assert.Null(Vars(run)["body"]);
                Assert.Equal("yes", Vars(run)["after"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task For_each_reads_a_list_from_an_earlier_step()
        {
            var loop = @"{ ""id"": ""l1"", ""kind"": ""action"", ""type"": ""loop"", ""name"": ""each"", ""config"": { ""items"": ""one\ntwo"" } }";
            var (provider, id) = await Build(
                Trigger + "," + loop + "," + SetVar("s1", "log", "{{vars.log}}{{loop.item}}|"),
                Edge("t1", "l1") + "," + Edge("l1", "s1", "each"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal("one|two|", Vars(run)["log"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task A_failed_card_with_an_error_path_runs_that_path_and_the_run_succeeds()
        {
            var (provider, id) = await Build(
                Trigger + "," + Failing("f1") + "," + SetVar("ok", "path", "normal") + "," + SetVar("err", "path", "error"),
                Edge("t1", "f1") + "," + Edge("f1", "ok") + "," + Edge("f1", "err", "error"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Succeeded, run.Status);
                Assert.Equal("error", Vars(run)["path"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task On_error_continue_goes_on_along_the_normal_path()
        {
            var (provider, id) = await Build(
                Trigger + "," + Failing("f1", @", ""onError"": ""continue""") + "," + SetVar("next", "path", "normal"),
                Edge("t1", "f1") + "," + Edge("f1", "next"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Succeeded, run.Status);
                Assert.Equal("normal", Vars(run)["path"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task A_failed_card_without_handling_fails_the_run_and_resume_queues_it_again()
        {
            var (provider, id) = await Build(
                Trigger + "," + Failing("f1") + "," + SetVar("next", "path", "normal"),
                Edge("t1", "f1") + "," + Edge("f1", "next"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Failed, run.Status);
                var runs = provider.GetRequiredService<IAutomationRunService>();
                var store = provider.GetRequiredService<IAutomationStore>();
                Assert.True(await runs.ResumeFailedAsync(run.Id));
                var again = (await store.GetRunAsync(run.Id))!;
                Assert.Equal(AutomationStatuses.Queued, again.Status);
                Assert.DoesNotContain("failed:", again.ChosenPortsJson);
                await runs.AdvanceAsync(run.Id);
                Assert.Equal(AutomationStatuses.Failed, (await store.GetRunAsync(run.Id))!.Status);
            }
        }

        [Fact]
        public async Task Only_continue_if_ends_the_branch_quietly_when_the_condition_is_false()
        {
            var filter = @"{ ""id"": ""f1"", ""kind"": ""action"", ""type"": ""filter"", ""name"": ""gate"", ""config"": { ""condition"": { ""op"": ""equals"", ""left"": ""no"", ""right"": ""yes"" } } }";
            var (provider, id) = await Build(
                Trigger + "," + filter + "," + SetVar("after", "reached", "yes"),
                Edge("t1", "f1") + "," + Edge("f1", "after"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Succeeded, run.Status);
                Assert.Null(Vars(run)["reached"]);
            }
        }

        [Fact]
        public async Task Only_continue_if_lets_the_flow_pass_when_the_condition_is_true()
        {
            var filter = @"{ ""id"": ""f1"", ""kind"": ""action"", ""type"": ""filter"", ""name"": ""gate"", ""config"": { ""condition"": { ""op"": ""equals"", ""left"": ""yes"", ""right"": ""yes"" } } }";
            var (provider, id) = await Build(
                Trigger + "," + filter + "," + SetVar("after", "reached", "yes"),
                Edge("t1", "f1") + "," + Edge("f1", "after"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal("yes", Vars(run)["reached"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task Agent_constants_are_available_as_vars()
        {
            var (provider, id) = await Build(
                Trigger + "," + SetVar("s1", "copy", "{{vars.company}}!"),
                Edge("t1", "s1"),
                @"{ ""variables"": { ""company"": ""Acme"" } }");
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal("Acme!", Vars(run)["copy"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task Rerun_starts_a_new_run_with_the_same_trigger_data()
        {
            var (provider, id) = await Build(Trigger + "," + SetVar("s1", "x", "1"), Edge("t1", "s1"));
            await using (provider)
            {
                var first = await RunLive(provider, id);
                var runs = provider.GetRequiredService<IAutomationRunService>();
                var second = await runs.RerunAsync(first.Id);
                Assert.NotEqual(first.Id, second.Id);
                Assert.Equal(first.VersionId, second.VersionId);
                Assert.Equal("rerun", second.TriggerReason);
            }
        }

        [Fact]
        public async Task Execute_workflow_can_wait_for_the_child_and_hand_over_its_variables()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var child = await authoring.CreateDraftAsync(owner, "child");
            await authoring.UpdateGraphAsync(child.Id, WorkflowGraph.Parse(
                @"{ ""schemaVersion"": 1, ""nodes"": [" + Trigger + "," + SetVar("c1", "answer", "42") + @"], ""edges"": [" + Edge("t1", "c1") + "] }"));
            await authoring.PublishAsync(child.Id);

            var parent = await authoring.CreateDraftAsync(owner, "parent");
            var call = @"{ ""id"": ""w1"", ""kind"": ""action"", ""type"": ""execute_workflow"", ""name"": ""callChild"", ""config"": { ""automationId"": """ + child.Id + @""", ""wait"": ""true"" } }";
            await authoring.UpdateGraphAsync(parent.Id, WorkflowGraph.Parse(
                @"{ ""schemaVersion"": 1, ""nodes"": [" + Trigger + "," + call + "," + SetVar("p1", "got", "{{steps.callChild.result.vars.answer}}") + @"], ""edges"": [" + Edge("t1", "w1") + "," + Edge("w1", "p1") + "] }"));
            await authoring.PublishAsync(parent.Id);

            var queued = await authoring.EnqueueRunAsync(parent.Id, "parent", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            var waiting = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Waiting, waiting.Status);

            var childRunId = Guid.Parse(JsonNode.Parse(waiting.ContextJson)!["steps"]!["callChild"]!["result"]!["runId"]!.GetValue<string>());
            await runs.AdvanceAsync(childRunId);
            Assert.Equal(AutomationStatuses.Succeeded, (await store.GetRunAsync(childRunId))!.Status);

            await runs.AdvanceAsync(queued.Id);
            var done = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Succeeded, done.Status);
            Assert.Equal("42", Vars(done)["got"]!.GetValue<string>());
        }

        [Fact]
        public async Task For_each_keeps_going_with_the_failed_item_after_resume()
        {
            // item "b" cannot be saved as a variable name: the loop stops there; the earlier items stay done
            var loop = @"{ ""id"": ""l1"", ""kind"": ""action"", ""type"": ""loop"", ""name"": ""each"", ""config"": { ""items"": ""[\""a\"",\""b bad\"",\""c\""]"" } }";
            var body = @"{ ""id"": ""s1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""s1"", ""config"": { ""name"": ""{{loop.item}}"", ""value"": ""x"" } }";
            var (provider, id) = await Build(Trigger + "," + loop + "," + body, Edge("t1", "l1") + "," + Edge("l1", "s1", "each"));
            await using (provider)
            {
                var run = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Failed, run.Status);
                Assert.Equal("x", Vars(run)["a"]!.GetValue<string>());
                Assert.Null(Vars(run)["c"]);
                var runs = provider.GetRequiredService<IAutomationRunService>();
                var store = provider.GetRequiredService<IAutomationStore>();
                Assert.True(await runs.ResumeFailedAsync(run.Id));
                await runs.AdvanceAsync(run.Id);
                // still failing on the same item: nothing from item "c" yet, and the loop did not restart from item "a"
                var again = (await store.GetRunAsync(run.Id))!;
                Assert.Equal(AutomationStatuses.Failed, again.Status);
                Assert.Null(Vars(again)["c"]);
            }
        }

        [Theory]
        [InlineData("{{ name | upper }}", "ANNA")]
        [InlineData("{{ name | lower | capitalize }}", "Anna")]
        [InlineData("{{ missing | default:\"n/a\" }}", "n/a")]
        [InlineData("{{ date | date:\"dd.MM.yyyy\" }}", "05.11.2026")]
        [InlineData("{{ price | round:1 }}", "12.3")]
        [InlineData("{{ price | number:\"0.000\" }}", "12.346")]
        [InlineData("{{ name | replace:\"n\",\"N\" }}", "aNNa")]
        [InlineData("{{ name | substring:1,2 }}", "nn")]
        [InlineData("{{ long | truncate:5 }}", "abcde...")]
        [InlineData("{{ list | length }}", "3")]
        [InlineData("{{ list | first }}", "x")]
        [InlineData("{{ list | join:\" + \" }}", "x + y + z")]
        [InlineData("{{ price | add:1 | round:0 }}", "13")]
        [InlineData("{{ name | upper | truncate:2 }}", "AN...")]
        public void Value_functions_work_in_fields(string template, string expected)
        {
            var binder = new VariableBinder();
            var variables = JsonNode.Parse(@"{ ""name"": ""anna"", ""date"": ""2026-11-05T10:00:00Z"", ""price"": 12.3456, ""long"": ""abcdefgh"", ""list"": [""x"",""y"",""z""] }")!.AsObject();
            Assert.Equal(expected, binder.Interpolate(template, variables));
        }

        [Fact]
        public void Validator_checks_for_each_cards()
        {
            var validator = new GraphValidator();
            var noEach = WorkflowGraph.Parse(@"{ ""schemaVersion"": 1, ""nodes"": [" + Trigger + @",
                { ""id"": ""l1"", ""kind"": ""action"", ""type"": ""loop"", ""name"": ""each"", ""config"": {} }],
                ""edges"": [" + Edge("t1", "l1") + "] }");
            Assert.Contains(validator.Validate(noEach), error => error.Contains("each", StringComparison.OrdinalIgnoreCase));

            var nested = WorkflowGraph.Parse(@"{ ""schemaVersion"": 1, ""nodes"": [" + Trigger + @",
                { ""id"": ""l1"", ""kind"": ""action"", ""type"": ""loop"", ""name"": ""outer"", ""config"": {} },
                { ""id"": ""l2"", ""kind"": ""action"", ""type"": ""loop"", ""name"": ""inner"", ""config"": {} },
                " + SetVar("s1", "a", "b") + @"],
                ""edges"": [" + Edge("t1", "l1") + "," + Edge("l1", "l2", "each") + "," + Edge("l2", "s1", "each") + "] }");
            Assert.Contains(validator.Validate(nested), error => error.Contains("inside another", StringComparison.OrdinalIgnoreCase));
        }
    }
}
