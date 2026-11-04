using System.Text.Json.Nodes;
using Partnerly.Automation.Engine;

namespace Partnerly.Integrations.Common
{
    // net6.0 has no JsonNode.DeepClone (added in .NET 8); same behavior via JsonNodes.Clone.
    public static class JsonNodeExtensions
    {
        public static JsonNode? DeepClone(this JsonNode node) => JsonNodes.Clone(node);
    }
}
