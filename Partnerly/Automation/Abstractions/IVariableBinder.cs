using System.Text.Json.Nodes;

namespace Partnerly.Automation.Abstractions
{
    public interface IVariableBinder
    {
        string Interpolate(string? template, JsonObject variables);
        JsonNode? Resolve(string? path, JsonObject variables);
        bool IsBlockedPath(string path);
    }
}
