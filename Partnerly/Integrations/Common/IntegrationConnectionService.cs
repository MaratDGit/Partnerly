using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Integrations.Common
{
    // Integration credentials reuse AutomationConnections: Kind tells the
    // service, EncryptedPayload holds tokens through ISecretProtector.
    public class IntegrationConnectionService : IIntegrationConnectionService
    {
        private readonly IAutomationStore _store;
        private readonly ISecretProtector _secrets;
        private readonly IAutomationClock _clock;

        public IntegrationConnectionService(IAutomationStore store, ISecretProtector secrets, IAutomationClock clock)
        {
            _store = store;
            _secrets = secrets;
            _clock = clock;
        }

        public Task<IReadOnlyList<IntegrationConnection>> ListAsync(
            Guid ownerUserId,
            string? kind = null,
            CancellationToken cancellationToken = default)
        {
            var query = _store.QueryConnections()
                .Where(item => !item.IsDeleted && item.OwnerUserId == ownerUserId);
            query = kind == null
                ? query.Where(item => IntegrationKinds.All.Contains(item.Kind))
                : query.Where(item => item.Kind == kind);

            IReadOnlyList<IntegrationConnection> list = query
                .OrderBy(item => item.Kind)
                .ThenBy(item => item.Name)
                .ToList()
                .Select(Open)
                .ToList();
            return Task.FromResult(list);
        }

        public async Task<IntegrationConnection> CreateAsync(
            Guid ownerUserId,
            string kind,
            string name,
            JsonObject payload,
            CancellationToken cancellationToken = default)
        {
            if (!IntegrationKinds.IsKnown(kind))
            {
                throw new ArgumentException("Unknown integration kind.", nameof(kind));
            }

            var now = _clock.UtcNow;
            var entity = new AutomationConnection
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                Name = string.IsNullOrWhiteSpace(name) ? kind : name.Trim(),
                Kind = kind,
                EncryptedPayload = _secrets.Protect(payload.ToJsonString()),
                CreatedBy = ownerUserId,
                CreatedDate = now,
                UpdatedBy = ownerUserId,
                UpdatedDate = now
            };
            await _store.AddConnectionAsync(entity, cancellationToken);
            return new IntegrationConnection(entity, payload);
        }

        public async Task<IntegrationConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _store.GetConnectionAsync(id, cancellationToken);
            return entity == null || entity.IsDeleted || !IntegrationKinds.IsKnown(entity.Kind) ? null : Open(entity);
        }

        public async Task<IntegrationConnection?> GetForOwnerAsync(
            Guid id,
            Guid ownerUserId,
            string kind,
            CancellationToken cancellationToken = default)
        {
            var connection = await GetAsync(id, cancellationToken);
            return connection == null || connection.OwnerUserId != ownerUserId || connection.Kind != kind
                ? null
                : connection;
        }

        public async Task SaveAsync(IntegrationConnection connection, CancellationToken cancellationToken = default)
        {
            connection.Entity.EncryptedPayload = _secrets.Protect(connection.Payload.ToJsonString());
            connection.Entity.UpdatedDate = _clock.UtcNow;
            await _store.UpdateConnectionAsync(connection.Entity, cancellationToken);
        }

        public async Task<bool> DeleteAsync(
            Guid id,
            Guid currentUserId,
            bool canManage,
            CancellationToken cancellationToken = default)
        {
            var entity = await _store.GetConnectionAsync(id, cancellationToken);
            if (entity == null
                || entity.IsDeleted
                || !IntegrationKinds.IsKnown(entity.Kind)
                || (!canManage && entity.OwnerUserId != currentUserId))
            {
                return false;
            }

            entity.IsDeleted = true;
            entity.UpdatedBy = currentUserId;
            entity.UpdatedDate = _clock.UtcNow;
            await _store.UpdateConnectionAsync(entity, cancellationToken);
            return true;
        }

        private IntegrationConnection Open(AutomationConnection entity)
        {
            JsonObject payload;
            try
            {
                var json = _secrets.Unprotect(entity.EncryptedPayload);
                payload = JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json) as JsonObject ?? new JsonObject();
            }
            catch
            {
                // Keys rotated or payload damaged: keep the row visible so the
                // user can reconnect, but with no secrets.
                payload = new JsonObject();
                payload["_status"] = new JsonObject { ["ok"] = false, ["error"] = "Stored credentials cannot be read. Reconnect." };
            }

            return new IntegrationConnection(entity, payload);
        }
    }
}
