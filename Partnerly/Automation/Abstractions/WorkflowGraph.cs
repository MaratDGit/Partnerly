using System.Text.Json;
using System.Text.Json.Serialization;

namespace Partnerly.Automation.Abstractions
{
    public class WorkflowGraph
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("nodes")]
        public List<WorkflowNode> Nodes { get; set; } = new();

        [JsonPropertyName("edges")]
        public List<WorkflowEdge> Edges { get; set; } = new();

        // Sticky notes on the editor canvas. Only for people reading the picture: the engine ignores them.
        [JsonPropertyName("notes")]
        public List<WorkflowNote>? Notes { get; set; }

        public static WorkflowGraph Parse(string json)
        {
            var graph = JsonSerializer.Deserialize<WorkflowGraph>(json, JsonOptions);
            return graph ?? new WorkflowGraph();
        }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public WorkflowNode? FindNode(string id) =>
            Nodes.FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.Ordinal));
    }

    public class WorkflowNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "action";

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("config")]
        public JsonElement Config { get; set; }

        // Turned off in the editor. A disabled action is skipped and the flow goes on to the
        // cards after it; a disabled trigger never starts the agent.
        [JsonPropertyName("disabled")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Disabled { get; set; }
    }

    public class WorkflowNote
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("w")]
        public double W { get; set; } = 200;

        [JsonPropertyName("h")]
        public double H { get; set; } = 120;

        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }

    public class WorkflowEdge
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public string? Port { get; set; }
    }
}
