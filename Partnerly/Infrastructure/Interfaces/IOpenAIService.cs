namespace Partnerly.Infrastructure.Interfaces
{
    public interface IOpenAIService
    {
        Task<string> AskAsync(
            string message,
            CancellationToken cancellationToken = default);
    }
}
