using Partnerly.Models.ViewModels;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IInsightsDashboardService
    {
        Task<DashboardsViewModel> BuildAsync(ISet<string> permissions, CancellationToken cancellationToken = default);
    }
}
