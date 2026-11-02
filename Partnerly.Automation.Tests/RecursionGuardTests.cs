using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Actions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Tests
{
    public class RecursionGuardTests
    {
        [Fact]
        public void Caps_depth_at_three()
        {
            Assert.Equal(0, RecursionGuard.Current);
            using (RecursionGuard.Enter())
            using (RecursionGuard.Enter())
            using (RecursionGuard.Enter())
            {
                Assert.Equal(3, RecursionGuard.Current);
                Assert.False(RecursionGuard.CanEnter);
            }

            Assert.Equal(0, RecursionGuard.Current);
            Assert.True(RecursionGuard.CanEnter);
        }

        [Fact]
        public async Task Execute_workflow_fails_when_depth_limit_reached()
        {
            var handler = new ExecuteWorkflowActionHandler();
            var result = await handler.ExecuteAsync(new ActionContext
            {
                Run = new AutomationRun { Id = Guid.NewGuid() },
                Node = new WorkflowNode { Id = "w1", Type = AutomationTypeCodes.ExecuteWorkflow, Name = "child" },
                Variables = new JsonObject(),
                Depth = RecursionGuard.MaxDepth,
                IsDryRun = false
            }, CancellationToken.None);

            Assert.Equal(AutomationStatuses.StepFailed, result.Status);
            Assert.Contains("depth", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}
