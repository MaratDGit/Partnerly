using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationSettingsViewModel
    {
        public AutomationDefinition? Definition { get; set; }
        public IReadOnlyList<AutomationDefinition> Definitions { get; set; } = Array.Empty<AutomationDefinition>();
    }
}
