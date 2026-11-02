namespace Partnerly.Automation.Abstractions
{
    public interface IActionHandler
    {
        string Type { get; }
        bool HasSideEffects { get; }
        HandlerDescriptor Describe();
        Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken);
    }
}
