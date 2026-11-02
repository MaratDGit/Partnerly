using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationHistoryPanelViewModel
    {
        public AutomationDefinition Definition { get; set; } = null!;
        public IReadOnlyList<AutomationRun> Runs { get; set; } = Array.Empty<AutomationRun>();
    }
}
