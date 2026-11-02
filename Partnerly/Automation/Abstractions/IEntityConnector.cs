using System.Text.Json.Nodes;

namespace Partnerly.Automation.Abstractions
{
    public interface IEntityConnector
    {
        string Key { get; }
        string DisplayName { get; }
        IReadOnlyList<EntityFieldDescriptor> Fields { get; }
        bool Can(string operation);
        Task<JsonObject?> FindAsync(JsonObject filters, Guid ownerUserId, CancellationToken cancellationToken);
        Task<JsonObject?> CreateAsync(JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken);
        Task<JsonObject?> UpdateAsync(JsonObject key, JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(JsonObject key, Guid ownerUserId, CancellationToken cancellationToken);
        JsonObject ToBag(object entity);
    }
}
