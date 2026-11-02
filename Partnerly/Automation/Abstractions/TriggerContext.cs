using System.Text.Json.Nodes;

namespace Partnerly.Automation.Abstractions
{
    public class TriggerContext
    {
        public string Type { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public Guid? OwnerUserId { get; set; }
        public JsonObject Payload { get; set; } = new();
    }
}
