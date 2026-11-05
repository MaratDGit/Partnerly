using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    // Runs of one card: every attempt of that card in the latest runs of the agent.
    public class AutomationNodeHistoryViewModel
    {
        public AutomationDefinition Definition { get; set; } = null!;
        public string NodeName { get; set; } = string.Empty;
        public List<NodeHistoryRow> Rows { get; set; } = new();
    }

    public class NodeHistoryRow
    {
        public Guid RunId { get; set; }
        public bool IsDryRun { get; set; }
        public string StartedText { get; set; } = string.Empty;
        public string StepStatus { get; set; } = string.Empty;
        public int Attempt { get; set; }
        public string? Error { get; set; }
    }
}
