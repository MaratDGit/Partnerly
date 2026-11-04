using System.Text.Json;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;

namespace Partnerly.Integrations.Common
{
    // One polling pass: finds connections that published agents listen to and
    // asks the matching poller for new items. Separate from the hosted worker
    // so tests can run a pass directly.
    public class IntegrationPollingService
    {
        private readonly IAutomationStore _store;
        private readonly IIntegrationConnectionService _connections;
        private readonly IEnumerable<IIntegrationPoller> _pollers;
        private readonly IAutomationClock _clock;

        public IntegrationPollingService(
            IAutomationStore store,
            IIntegrationConnectionService connections,
            IEnumerable<IIntegrationPoller> pollers,
            IAutomationClock clock)
        {
            _store = store;
            _connections = connections;
            _pollers = pollers;
            _clock = clock;
        }

        public async Task<int> PollOnceAsync(
            Func<IIntegrationPoller, bool>? isDue = null,
            CancellationToken cancellationToken = default)
        {
            var polled = 0;
            foreach (var poller in _pollers)
            {
                if (isDue != null && !isDue(poller))
                {
                    continue;
                }

                var armed = await _store.GetEnabledTriggersAsync(poller.TriggerType, cancellationToken);
                var groups = armed
                    .Select(item => new { item, config = Parse(item.Trigger.ConfigJson) })
                    .Select(x => new { x.item, x.config, id = ReadGuid(x.config, "connectionId") })
                    .Where(x => x.id != null)
                    .GroupBy(x => x.id!.Value);

                foreach (var group in groups)
                {
                    var connection = await _connections.GetAsync(group.Key, cancellationToken);
                    if (connection == null || connection.Kind != poller.ConnectionKind)
                    {
                        continue;
                    }

                    // Only triggers of the connection owner count (IDOR guard).
                    var configs = group
                        .Where(x => x.item.Definition.OwnerUserId == connection.OwnerUserId)
                        .Select(x => x.config)
                        .ToList();
                    if (configs.Count == 0)
                    {
                        continue;
                    }

                    try
                    {
                        await poller.PollAsync(new IntegrationPollTarget(connection, configs), cancellationToken);
                        connection.MarkOk(_clock.UtcNow);
                    }
                    catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }

                        connection.MarkFailed(_clock.UtcNow, ex.Message);
                    }

                    await _connections.SaveAsync(connection, cancellationToken);
                    polled++;
                }
            }

            return polled;
        }

        private static JsonElement Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static Guid? ReadGuid(JsonElement config, string name)
        {
            if (config.ValueKind == JsonValueKind.Object
                && config.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out var id))
            {
                return id;
            }

            return null;
        }
    }
}
