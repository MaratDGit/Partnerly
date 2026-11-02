using System.Text.Json.Nodes;

namespace Partnerly.Automation.Engine
{
    public static class JsonNodes
    {
        public static JsonNode? Clone(JsonNode? node)
        {
            return node == null ? null : JsonNode.Parse(node.ToJsonString());
        }

        public static bool EqualsNode(JsonNode? left, JsonNode? right)
        {
            if (left is null && right is null)
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return string.Equals(left.ToJsonString(), right.ToJsonString(), StringComparison.Ordinal);
        }
    }
}
