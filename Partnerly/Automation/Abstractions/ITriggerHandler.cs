namespace Partnerly.Automation.Abstractions
{
    public interface ITriggerHandler
    {
        string Type { get; }
        HandlerDescriptor Describe();
        bool CanStart(TriggerContext context);
    }
}
