using Partnerly.Models.ViewModels;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface ISiteSearchService
    {
        Task<SiteSearchResult> SearchAsync(
            string? query,
            ISet<string> permissions,
            CancellationToken cancellationToken = default);
    }
}
