using Partnerly.Automation.Abstractions;
using Partnerly.Helpers;

namespace Partnerly.Automation.Security
{
    public class AutomationHttpGuard : IPublicHttpGuard
    {
        public bool IsAllowed(string? url) => PublicHttpGuard.IsAllowed(url);

        public Task<bool> IsAllowedAsync(string? url, CancellationToken cancellationToken = default) =>
            PublicHttpGuard.IsAllowedAsync(url, cancellationToken);
    }
}
