using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationRunDetailsViewModel
    {
        public AutomationRun Run { get; set; } = null!;
        public AutomationDefinition Definition { get; set; } = null!;
        public IReadOnlyList<AutomationRunStep> Steps { get; set; } = Array.Empty<AutomationRunStep>();
        // Card names from the graph of the version that ran (node id -> name).
        public Dictionary<string, string> NodeNames { get; set; } = new();
    }
}
