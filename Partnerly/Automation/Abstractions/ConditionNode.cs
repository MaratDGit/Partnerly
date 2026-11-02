using System.Text.Json.Serialization;

namespace Partnerly.Automation.Abstractions
{
    public class ConditionNode
    {
        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        [JsonPropertyName("left")]
        public string? Left { get; set; }

        [JsonPropertyName("right")]
        public string? Right { get; set; }

        [JsonPropertyName("children")]
        public List<ConditionNode> Children { get; set; } = new();
    }
}
