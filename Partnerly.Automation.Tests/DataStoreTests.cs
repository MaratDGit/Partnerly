using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Tests
{
    // Data store cards (save / read / forget), "Only once", tags.
    public class DataStoreTests
    {
        private const string Trigger = @"{ ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} }";

        private static string Card(string id, string type, string config) =>
            @"{ ""id"": """ + id + @""", ""kind"": ""action"", ""type"": """ + type + @""", ""name"": """ + id + @""", ""config"": " + config + " }";

        private static string Edge(string from, string to) =>
            @"{ ""id"": ""e_" + from + "_" + to + @""", ""from"": """ + from + @""", ""to"": """ + to + @""" }";

        private static async Task<(ServiceProvider Provider, Guid Id)> Build(string nodes, string edges)
        {
            var owner = Guid.NewGuid();
            var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var automation = await authoring.CreateDraftAsync(owner, "store");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(
                @"{ ""schemaVersion"": 1, ""nodes"": [" + nodes + @"], ""edges"": [" + edges + "] }"));
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
        public async Task A_saved_value_is_read_by_the_next_run()
        {
            var (provider, id) = await Build(
                Trigger + "," +
                Card("get", "store_get", @"{ ""key"": ""counter"", ""default"": ""0"" }") + "," +
                Card("save", "store_set", @"{ ""key"": ""counter"", ""value"": ""{{steps.get.result.value}}x"" }") + "," +
                Card("copy", "set_variable", @"{ ""name"": ""seen"", ""value"": ""{{steps.get.result.value}}"" }"),
                Edge("t1", "get") + "," + Edge("get", "save") + "," + Edge("save", "copy"));
            await using (provider)
            {
                Assert.Equal("0", Vars(await RunLive(provider, id))["seen"]!.GetValue<string>());
                Assert.Equal("0x", Vars(await RunLive(provider, id))["seen"]!.GetValue<string>());
                Assert.Equal("0xx", Vars(await RunLive(provider, id))["seen"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task Only_once_lets_a_key_through_the_first_time_only()
        {
            var (provider, id) = await Build(
                Trigger + "," +
                Card("once", "once", @"{ ""key"": ""order-7"" }") + "," +
                Card("work", "set_variable", @"{ ""name"": ""handled"", ""value"": ""yes"" }"),
                Edge("t1", "once") + "," + Edge("once", "work"));
            await using (provider)
            {
                var first = await RunLive(provider, id);
                Assert.Equal("yes", Vars(first)["handled"]!.GetValue<string>());
                var second = await RunLive(provider, id);
                Assert.Equal(AutomationStatuses.Succeeded, second.Status);
                Assert.Null(Vars(second)["handled"]);
            }
        }

        [Fact]
        public async Task A_test_run_does_not_use_up_the_only_once_key()
        {
            var (provider, id) = await Build(
                Trigger + "," + Card("once", "once", @"{ ""key"": ""k"" }") + "," + Card("work", "set_variable", @"{ ""name"": ""handled"", ""value"": ""yes"" }"),
                Edge("t1", "once") + "," + Edge("once", "work"));
            await using (provider)
            {
                var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
                var runs = provider.GetRequiredService<IAutomationRunService>();
                var store = provider.GetRequiredService<IAutomationStore>();
                var test = await authoring.EnqueueRunAsync(id, "test", dryRun: true);
                await runs.AdvanceAsync(test.Id);
                Assert.Equal("yes", Vars((await store.GetRunAsync(test.Id))!)["handled"]!.GetValue<string>());
                Assert.Equal("yes", Vars(await RunLive(provider, id))["handled"]!.GetValue<string>());
            }
        }

        [Fact]
        public async Task Forgetting_a_value_and_expiry_work()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var store = provider.GetRequiredService<IAutomationStore>();
            var agent = Guid.NewGuid();
            var now = new DateTime(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

            await store.SetDataItemAsync(agent, "a", "1", now, now.AddHours(1));
            Assert.Equal("1", (await store.GetDataItemAsync(agent, "a", now.AddMinutes(30)))!.Value);
            Assert.Null(await store.GetDataItemAsync(agent, "a", now.AddHours(2)));
            Assert.True(await store.TryAddDataItemAsync(agent, "a", "2", now.AddHours(2), null));
            Assert.False(await store.TryAddDataItemAsync(agent, "a", "3", now.AddHours(3), null));
            Assert.True(await store.DeleteDataItemAsync(agent, "a"));
            Assert.Null(await store.GetDataItemAsync(agent, "a", now));
            Assert.Null(await store.GetDataItemAsync(Guid.NewGuid(), "a", now));
        }

        [Theory]
        [InlineData(" Sales , weekly;sales ", "Sales,weekly")]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("a,b,c,d,e,f,g,h,i,j", "a,b,c,d,e,f,g,h")]
        public void Tags_are_cleaned(string? raw, string? expected)
        {
            Assert.Equal(expected, AutomationDefinition.NormalizeTags(raw));
        }
    }
}
