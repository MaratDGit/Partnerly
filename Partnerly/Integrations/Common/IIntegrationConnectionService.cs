using System.Text.Json.Nodes;

namespace Partnerly.Integrations.Common
{
    public interface IIntegrationConnectionService
    {
        Task<IReadOnlyList<IntegrationConnection>> ListAsync(Guid ownerUserId, string? kind = null, CancellationToken cancellationToken = default);
        Task<IntegrationConnection> CreateAsync(Guid ownerUserId, string kind, string name, JsonObject payload, CancellationToken cancellationToken = default);
        Task<IntegrationConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IntegrationConnection?> GetForOwnerAsync(Guid id, Guid ownerUserId, string kind, CancellationToken cancellationToken = default);
        Task SaveAsync(IntegrationConnection connection, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, Guid currentUserId, bool canManage, CancellationToken cancellationToken = default);
    }
}
