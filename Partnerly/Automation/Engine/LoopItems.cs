using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Engine
{
    // The list a "For each" card goes through: a path to a list ({{steps.read.result.rows}}),
    // a JSON list text, or plain lines (one item per line).
    public static class LoopItems
    {
        public const int DefaultMax = 100;
        public const int HardMax = 500;

        public static bool TryRead(WorkflowNode node, IVariableBinder binder, JsonObject variables, out JsonArray items, out string error)
        {
            items = new JsonArray();
            error = string.Empty;
            var raw = NodeConfig.GetString(node, "items");
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "For each needs a list.";
                return false;
            }

            var max = DefaultMax;
            if (int.TryParse(NodeConfig.GetString(node, "maxItems"), out var configured) && configured > 0)
            {
                max = Math.Min(configured, HardMax);
            }

            var text = raw.Trim();
            JsonNode? source = null;
            if (text.StartsWith("{{", StringComparison.Ordinal) && text.EndsWith("}}", StringComparison.Ordinal)
                && text.IndexOf("}}", 2, StringComparison.Ordinal) == text.Length - 2)
            {
                source = binder.Resolve(text, variables);
            }
            else
            {
                var filled = binder.Interpolate(text, variables).Trim();
                try
                {
                    source = JsonNode.Parse(filled);
                }
                catch (JsonException)
                {
                    source = null;
                }

                if (source is not JsonArray)
                {
                    var lines = filled.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var array = new JsonArray();
                    foreach (var line in lines)
                    {
                        array.Add(line);
                    }

                    source = array;
                }
            }

            if (source is JsonValue value && value.TryGetValue<string>(out var asText))
            {
                try
                {
                    source = JsonNode.Parse(asText);
                }
                catch (JsonException)
                {
                    source = null;
                }
            }

            if (source is not JsonArray list)
            {
                error = "For each: the value is not a list.";
                return false;
            }

            foreach (var item in list.Take(max))
            {
                items.Add(JsonNodes.Clone(item));
            }

            return true;
        }
    }
}
