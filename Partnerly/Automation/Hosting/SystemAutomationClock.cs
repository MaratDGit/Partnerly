using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Hosting
{
    public class SystemAutomationClock : IAutomationClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
