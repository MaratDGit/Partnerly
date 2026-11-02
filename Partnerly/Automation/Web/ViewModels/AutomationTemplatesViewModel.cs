using Partnerly.Automation.Authoring;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationTemplatesViewModel
    {
        public IReadOnlyList<AutomationPreset> Presets { get; set; } = Array.Empty<AutomationPreset>();
    }
}
