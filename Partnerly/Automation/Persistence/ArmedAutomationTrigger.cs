using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence
{
    public class ArmedAutomationTrigger
    {
        public AutomationTrigger Trigger { get; set; } = null!;
        public AutomationDefinition Definition { get; set; } = null!;
        public AutomationVersion Version { get; set; } = null!;
    }
}
