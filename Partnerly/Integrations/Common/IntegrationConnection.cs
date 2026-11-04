using System.Text.Json.Nodes;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Integrations.Common
{
    // An AutomationConnection with its decrypted payload. The payload holds
    // tokens, so it must never reach logs, step output or the browser.
    public class IntegrationConnection
    {
        public IntegrationConnection(AutomationConnection entity, JsonObject payload)
        {
            Entity = entity;
            Payload = payload;
        }

        public AutomationConnection Entity { get; }
        public JsonObject Payload { get; }

        public Guid Id => Entity.Id;
        public Guid OwnerUserId => Entity.OwnerUserId;
        public string Kind => Entity.Kind;
        public string Name => Entity.Name;

        public string? GetString(string name) =>
            Payload[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : Payload[name]?.ToString();

        public void Set(string name, string? value)
        {
            if (value == null)
            {
                Payload.Remove(name);
            }
            else
            {
                Payload[name] = value;
            }
        }

        // Status lives next to the secrets so no migration is needed (plan D2).
        public IntegrationStatus Status
        {
            get
            {
                var node = Payload["_status"] as JsonObject;
                return new IntegrationStatus
                {
                    Ok = node?["ok"]?.GetValue<bool>() ?? true,
                    CheckedAtUtc = DateTime.TryParse(node?["at"]?.GetValue<string>(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var at) ? at : null,
                    Error = node?["error"]?.GetValue<string>()
                };
            }
        }

        public void MarkOk(DateTime utcNow)
        {
            Payload["_status"] = new JsonObject { ["ok"] = true, ["at"] = utcNow.ToString("O") };
        }

        public void MarkFailed(DateTime utcNow, string error)
        {
            Payload["_status"] = new JsonObject
            {
                ["ok"] = false,
                ["at"] = utcNow.ToString("O"),
                ["error"] = error.Length > 300 ? error[..300] : error
            };
        }
    }

    public class IntegrationStatus
    {
        public bool Ok { get; set; }
        public DateTime? CheckedAtUtc { get; set; }
        public string? Error { get; set; }
    }
}
