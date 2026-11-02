namespace Partnerly.Automation.Abstractions
{
    public interface IPublicHttpGuard
    {
        bool IsAllowed(string? url);
        Task<bool> IsAllowedAsync(string? url, CancellationToken cancellationToken = default);
    }
}
