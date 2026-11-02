namespace Partnerly.Automation.Abstractions
{
    public interface IAutomationClock
    {
        DateTime UtcNow { get; }
    }
}
