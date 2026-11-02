using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;

namespace Partnerly.Automation.Tests
{
    public class AccessControlTests
    {
        [Fact]
        public async Task Other_user_cannot_read_or_mutate_automation()
        {
            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            var store = new InMemoryAutomationStore();
            await using var ownerHost = AutomationTestHost.Create(owner, store: store);
            var ownerAuthoring = ownerHost.GetRequiredService<IAutomationAuthoringService>();
            var created = await ownerAuthoring.CreateDraftAsync(owner, "secret");
            await ownerAuthoring.UpdateGraphAsync(created.Id, LinearManualGraph());

            await using var otherHost = AutomationTestHost.Create(other, store: store);
            var otherAuthoring = otherHost.GetRequiredService<IAutomationAuthoringService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.GetAsync(created.Id));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.GetDraftVersionAsync(created.Id));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.UpdateMetaAsync(created.Id, "hacked", null));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.UpdateGraphAsync(created.Id, LinearManualGraph()));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.PublishAsync(created.Id));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.ToggleActiveAsync(created.Id));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.EnqueueRunAsync(created.Id, "steal", dryRun: true));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.CopyAsync(created.Id, other));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.DeleteAsync(created.Id));

            var untouched = await ownerAuthoring.GetAsync(created.Id);
            Assert.Equal("secret", untouched.Name);
            Assert.False(untouched.IsDeleted);
        }

        [Fact]
        public async Task Manage_user_can_open_and_copy_foreign_automation()
        {
            var owner = Guid.NewGuid();
            var admin = Guid.NewGuid();
            var store = new InMemoryAutomationStore();
            await using var ownerHost = AutomationTestHost.Create(owner, store: store);
            var created = await ownerHost.GetRequiredService<IAutomationAuthoringService>()
                .CreateDraftAsync(owner, "owned");

            await using var adminHost = AutomationTestHost.Create(admin, canManage: true, store: store);
            var adminAuthoring = adminHost.GetRequiredService<IAutomationAuthoringService>();
            var visible = await adminAuthoring.GetAsync(created.Id);
            Assert.Equal(owner, visible.OwnerUserId);

            var copy = await adminAuthoring.CopyAsync(created.Id, admin);
            Assert.Equal(admin, copy.OwnerUserId);
            Assert.Contains("copy", copy.Name, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Deleted_automation_is_hidden_from_owner_and_manage()
        {
            var owner = Guid.NewGuid();
            var admin = Guid.NewGuid();
            var store = new InMemoryAutomationStore();
            await using var ownerHost = AutomationTestHost.Create(owner, store: store);
            var ownerAuthoring = ownerHost.GetRequiredService<IAutomationAuthoringService>();
            var created = await ownerAuthoring.CreateDraftAsync(owner, "gone");
            await ownerAuthoring.DeleteAsync(created.Id);

            var persisted = await store.GetDefinitionAsync(created.Id);
            Assert.True(persisted!.IsDeleted);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => ownerAuthoring.GetAsync(created.Id));

            await using var adminHost = AutomationTestHost.Create(admin, canManage: true, store: store);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                adminHost.GetRequiredService<IAutomationAuthoringService>().GetAsync(created.Id));
        }

        private static WorkflowGraph LinearManualGraph() => WorkflowGraph.Parse(@"{
          ""schemaVersion"": 1,
          ""nodes"": [
            { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
            { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""flag"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
          ],
          ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
        }");
    }
}
