using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Integration
{
    public class AutomationTriggerDispatcher
    {
        private readonly IAutomationStore _store;
        private readonly IAutomationRunService _runs;
        private readonly IAutomationClock _clock;

        public AutomationTriggerDispatcher(
            IAutomationStore store,
            IAutomationRunService runs,
            IAutomationClock clock)
        {
            _store = store;
            _runs = runs;
            _clock = clock;
        }

        public Task EnqueueEntityAsync(
            string triggerType,
            string entityKey,
            JsonObject payload,
            CancellationToken cancellationToken = default)
        {
            return EnqueueAsync(triggerType, payload, config => MatchesEntity(config, entityKey), cancellationToken);
        }

        public Task EnqueueEventAsync(
            string eventName,
            JsonObject payload,
            CancellationToken cancellationToken = default)
        {
            return EnqueueAsync(AutomationTypeCodes.Event, payload, config => MatchesEvent(config, eventName), cancellationToken);
        }

        // Integration triggers carry a connectionId in their config. Only the
        // owner's automations bound to that connection may start, so one
        // user's Telegram bot never wakes another user's agents.
        public async Task EnqueueIntegrationAsync(
            string triggerType,
            Guid ownerUserId,
            Guid connectionId,
            JsonObject payload,
            Func<JsonElement, bool>? matches = null,
            CancellationToken cancellationToken = default)
        {
            var armed = await _store.GetEnabledTriggersAsync(triggerType, cancellationToken);
            foreach (var item in armed)
            {
                if (item.Definition.OwnerUserId != ownerUserId)
                {
                    continue;
                }

                var config = ParseConfig(item.Trigger.ConfigJson);
                if (!Guid.TryParse(ReadString(config, "connectionId"), out var boundId) || boundId != connectionId)
                {
                    continue;
                }

                if (matches != null && !matches(config))
                {
                    continue;
                }

                await TryEnqueueAsync(item, triggerType, payload, cancellationToken);
            }
        }

        public Task EnqueueArmedAsync(
            ArmedAutomationTrigger armed,
            JsonObject payload,
            CancellationToken cancellationToken = default)
        {
            return TryEnqueueAsync(armed, armed.Trigger.Type, payload, cancellationToken);
        }

        private async Task EnqueueAsync(
            string triggerType,
            JsonObject payload,
            Func<JsonElement, bool> matches,
            CancellationToken cancellationToken)
        {
            var armed = await _store.GetEnabledTriggersAsync(triggerType, cancellationToken);
            foreach (var item in armed)
            {
                var config = ParseConfig(item.Trigger.ConfigJson);
                if (!matches(config))
                {
                    continue;
                }

                await TryEnqueueAsync(item, triggerType, payload, cancellationToken);
            }
        }

        private async Task TryEnqueueAsync(
            ArmedAutomationTrigger item,
            string triggerType,
            JsonObject payload,
            CancellationToken cancellationToken)
        {
            var active = await _store.CountActiveRunsAsync(item.Definition.Id, cancellationToken);
            if (active >= item.Definition.MaxConcurrentRuns)
            {
                return;
            }

            var recent = await _store.CountRunsSinceAsync(item.Definition.Id, _clock.UtcNow.AddHours(-1), cancellationToken);
            if (recent >= item.Definition.MaxRunsPerHour)
            {
                return;
            }

            var run = await _runs.EnqueueAsync(
                item.Definition,
                item.Version,
                triggerType,
                triggerType,
                dryRun: false,
                new TriggerContext
                {
                    Type = triggerType,
                    OwnerUserId = item.Definition.OwnerUserId,
                    Payload = JsonNodes.Clone(payload) as JsonObject ?? new JsonObject()
                },
                cancellationToken);

            var due = _clock.UtcNow;
            var startAfter = ReadStartAfter(ParseConfig(item.Trigger.ConfigJson));
            if (startAfter > TimeSpan.Zero)
            {
                due = due.Add(startAfter);
            }

            await _store.AddWakeupAsync(new AutomationWakeup
            {
                Id = Guid.NewGuid(),
                Kind = AutomationStatuses.WakeupStart,
                DueAtUtc = due,
                AutomationId = item.Definition.Id,
                VersionId = item.Version.Id,
                TriggerId = item.Trigger.Id,
                RunId = run.Id,
                Status = AutomationStatuses.WakeupPending
            }, cancellationToken);
        }

        private static bool MatchesEntity(JsonElement config, string entityKey)
        {
            var entity = ReadString(config, "entity") ?? ReadString(config, "entityKey");
            return string.Equals(entity, entityKey, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesEvent(JsonElement config, string eventName)
        {
            var name = ReadString(config, "event") ?? ReadString(config, "eventName");
            return string.Equals(name, eventName, StringComparison.OrdinalIgnoreCase);
        }

        private static JsonElement ParseConfig(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static string? ReadString(JsonElement config, string name)
        {
            if (config.ValueKind != JsonValueKind.Object || !config.TryGetProperty(name, out var value))
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        private static TimeSpan ReadStartAfter(JsonElement config)
        {
            return DelayDuration.TryRead(config, out var delay) ? delay : TimeSpan.Zero;
        }
    }
}
