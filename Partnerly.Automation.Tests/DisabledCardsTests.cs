using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Tests
{
    // Cards turned off in the editor: the flow passes through them.
    public class DisabledCardsTests
    {
        private static string SetVar(string id, string name, string variable, string value, bool disabled = false) =>
            $@"{{ ""id"": ""{id}"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""{name}"", ""disabled"": {(disabled ? "true" : "false")}, ""config"": {{ ""name"": ""{variable}"", ""value"": ""{value}"" }} }}";

        private static async Task<(AutomationRun run, IReadOnlyList<AutomationRunStep> steps)> RunGraphAsync(string nodes, string edges)
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var automation = await authoring.CreateDraftAsync(owner, "off");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(
                $@"{{ ""schemaVersion"": 1, ""nodes"": [ {{ ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {{}} }}, {nodes} ], ""edges"": [ {edges} ] }}"));
            var queued = await authoring.EnqueueRunAsync(automation.Id, "test", true);
            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            return (run, (await store.GetStepsAsync(run.Id)).ToList());
        }

        [Fact]
        public async Task A_disabled_card_in_the_middle_is_skipped_and_the_flow_goes_on()
        {
            var (run, steps) = await RunGraphAsync(
                SetVar("a", "first", "one", "1") + "," + SetVar("b", "middle", "two", "2", disabled: true) + "," + SetVar("c", "last", "three", "3"),
                @"{ ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a"" }, { ""id"": ""e2"", ""from"": ""a"", ""to"": ""b"" }, { ""id"": ""e3"", ""from"": ""b"", ""to"": ""c"" }");

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var vars = JsonNode.Parse(run.ContextJson)!["vars"]!;
            Assert.Equal("1", vars["one"]!.ToString());
            Assert.Null(vars["two"]);
            Assert.Equal("3", vars["three"]!.ToString());
            Assert.Equal(AutomationStatuses.StepSkipped, steps.Single(step => step.NodeId == "b").Status);
            Assert.Contains("disabled", steps.Single(step => step.NodeId == "b").OutputJson);
        }

        [Fact]
        public async Task A_disabled_card_with_several_inputs_and_outputs_connects_them_all()
        {
            // a and b both lead into x (off); x leads into c and d.
            var (run, _) = await RunGraphAsync(
                SetVar("a", "a1", "va", "A") + "," + SetVar("b", "b1", "vb", "B") + "," + SetVar("x", "off1", "vx", "X", disabled: true) + "," +
                SetVar("c", "c1", "vc", "C") + "," + SetVar("d", "d1", "vd", "D"),
                @"{ ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a"" }, { ""id"": ""e2"", ""from"": ""t1"", ""to"": ""b"" },
                  { ""id"": ""e3"", ""from"": ""a"", ""to"": ""x"" }, { ""id"": ""e4"", ""from"": ""b"", ""to"": ""x"" },
                  { ""id"": ""e5"", ""from"": ""x"", ""to"": ""c"" }, { ""id"": ""e6"", ""from"": ""x"", ""to"": ""d"" }");

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var vars = JsonNode.Parse(run.ContextJson)!["vars"]!;
            Assert.Equal("A", vars["va"]!.ToString());
            Assert.Equal("B", vars["vb"]!.ToString());
            Assert.Null(vars["vx"]);
            Assert.Equal("C", vars["vc"]!.ToString());
            Assert.Equal("D", vars["vd"]!.ToString());
        }

        [Fact]
        public async Task A_disabled_branch_follows_only_its_first_output()
        {
            var branch = @"{ ""id"": ""br"", ""kind"": ""action"", ""type"": ""branch"", ""name"": ""check"", ""disabled"": true,
                             ""config"": { ""condition"": { ""op"": ""equals"", ""left"": ""x"", ""right"": ""y"" } } }";
            var (run, _) = await RunGraphAsync(
                branch + "," + SetVar("ok", "yes", "pathYes", "1") + "," + SetVar("no", "no1", "pathNo", "1"),
                @"{ ""id"": ""e1"", ""from"": ""t1"", ""to"": ""br"" },
                  { ""id"": ""e2"", ""from"": ""br"", ""to"": ""ok"", ""port"": ""true"" },
                  { ""id"": ""e3"", ""from"": ""br"", ""to"": ""no"", ""port"": ""false"" }");

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var vars = JsonNode.Parse(run.ContextJson)!["vars"]!;
            Assert.Equal("1", vars["pathYes"]!.ToString());
            Assert.Null(vars["pathNo"]);
        }

        [Fact]
        public void Disabled_flag_survives_the_json_round_trip_and_is_omitted_when_off()
        {
            var graph = WorkflowGraph.Parse(@"{ ""schemaVersion"": 1, ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""disabled"": true, ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""delay"", ""name"": ""wait"", ""config"": {} } ], ""edges"": [] }");

            var json = graph.ToJson();

            Assert.True(graph.Nodes[0].Disabled);
            Assert.False(graph.Nodes[1].Disabled);
            Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(json, "\"disabled\"").Count);
        }

        [Fact]
        public void Validator_needs_one_trigger_turned_on_and_ignores_the_connection_of_an_off_card()
        {
            var validator = new GraphValidator();
            var allOff = WorkflowGraph.Parse(@"{ ""schemaVersion"": 1, ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""disabled"": true, ""config"": {} } ], ""edges"": [] }");
            var oneOn = WorkflowGraph.Parse(@"{ ""schemaVersion"": 1, ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""disabled"": true, ""config"": {} },
                { ""id"": ""t2"", ""kind"": ""trigger"", ""type"": ""webhook"", ""name"": ""hook"", ""config"": {} } ], ""edges"": [] }");

            Assert.Contains(validator.Validate(allOff), error => error.Contains("Turn on at least one trigger"));
            Assert.Empty(validator.Validate(oneOn));
        }

        [Fact]
        public async Task A_disabled_trigger_is_published_as_not_enabled()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var automation = await authoring.CreateDraftAsync(owner, "two");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{ ""schemaVersion"": 1, ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""t2"", ""kind"": ""trigger"", ""type"": ""webhook"", ""name"": ""hook"", ""disabled"": true, ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""x"", ""config"": { ""name"": ""v"", ""value"": ""1"" } } ],
                ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" }, { ""id"": ""e2"", ""from"": ""t2"", ""to"": ""a1"" } ] }"));
            await authoring.PublishAsync(automation.Id);

            var definition = await authoring.GetAsync(automation.Id);
            var triggers = await store.GetTriggersAsync(definition.PublishedVersionId!.Value);

            Assert.True(triggers.Single(item => item.Type == AutomationTypeCodes.Manual).IsEnabled);
            Assert.False(triggers.Single(item => item.Type == AutomationTypeCodes.Webhook).IsEnabled);
        }
    }
}
