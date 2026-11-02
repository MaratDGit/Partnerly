using Partnerly.Automation.Authoring;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationDashboardViewModel
    {
        public IReadOnlyList<AutomationDefinition> Definitions { get; set; } = Array.Empty<AutomationDefinition>();
        public IReadOnlyList<AutomationPreset> Presets { get; set; } = Array.Empty<AutomationPreset>();
    }
}
