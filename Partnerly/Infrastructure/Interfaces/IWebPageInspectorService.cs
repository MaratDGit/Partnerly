using Partnerly.Models.ViewModels;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IWebPageInspectorService
    {
        Task<WebPageInspectResult> InspectAsync(string? url, CancellationToken cancellationToken = default);
    }
}
