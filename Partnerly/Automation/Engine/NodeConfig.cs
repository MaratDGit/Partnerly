using System.Text.Json;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Engine
{
    public static class NodeConfig
    {
        public static T? Get<T>(WorkflowNode node)
        {
            if (node.Config.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return default;
            }

            return node.Config.Deserialize<T>(WorkflowGraph.JsonOptions);
        }

        public static string? GetString(WorkflowNode node, string name)
        {
            if (node.Config.ValueKind != JsonValueKind.Object
                || !node.Config.TryGetProperty(name, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
                _ => null
            };
        }

        public static int[] GetIntArray(WorkflowNode node, string name)
        {
            if (node.Config.ValueKind != JsonValueKind.Object
                || !node.Config.TryGetProperty(name, out var value)
                || value.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<int>();
            }

            var items = new List<int>();
            foreach (var element in value.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
                {
                    items.Add(number);
                }
            }

            return items.ToArray();
        }
    }
}
