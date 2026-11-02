using System.Text.Json.Nodes;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Abstractions
{
    public class ActionContext
    {
        public AutomationRun Run { get; set; } = null!;
        public WorkflowNode Node { get; set; } = null!;
        public JsonObject Variables { get; set; } = new();
        public bool IsDryRun { get; set; }
        public int Depth { get; set; }
        public DateTime UtcNow { get; set; }
        public IServiceProvider Services { get; set; } = null!;
    }
}
