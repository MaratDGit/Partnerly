using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Engine
{
    public class VariableBinder : IVariableBinder
    {
        private static readonly Regex Placeholder = new(@"\{\{\s*([^}]+?)\s*\}\}", RegexOptions.Compiled);
        private static readonly HashSet<string> BlockedSegments = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "passwordhash", "passwordHash",
            "googleid", "appleid", "clientsecret", "privatekey",
            "smtppassword", "apikey", "token", "secret", "connectionstring"
        };

        public bool IsBlockedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return path.Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => BlockedSegments.Contains(segment.Trim()));
        }

        public string Interpolate(string? template, JsonObject variables)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template ?? string.Empty;
            }

            return Placeholder.Replace(template, match =>
            {
                var path = match.Groups[1].Value.Trim();
                var node = Resolve(path, variables);
                return NodeToString(node);
            });
        }

        public JsonNode? Resolve(string? path, JsonObject variables)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var trimmed = path.Trim();
            if (trimmed.StartsWith("{{", StringComparison.Ordinal) && trimmed.EndsWith("}}", StringComparison.Ordinal))
            {
                trimmed = trimmed[2..^2].Trim();
            }

            // functions after the path: {{ steps.a.result.name | upper | truncate:20 }}
            var calls = new List<ValueFilters.Call>();
            if (trimmed.Contains('|'))
            {
                trimmed = ValueFilters.Split(trimmed, calls);
            }

            if (IsBlockedPath(trimmed))
            {
                return null;
            }

            JsonNode? current = variables;
            foreach (var segment in trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current is JsonObject obj && obj.TryGetPropertyValue(segment, out var next))
                {
                    current = next;
                    continue;
                }

                current = null;
                break;
            }

            return calls.Count == 0 ? current : ValueFilters.Apply(current == null ? null : JsonNodes.Clone(current), calls);
        }

        public static string NodeToString(JsonNode? node)
        {
            if (node is null)
            {
                return string.Empty;
            }

            if (node is JsonValue value)
            {
                if (value.TryGetValue<string>(out var text))
                {
                    return text;
                }

                if (value.TryGetValue<bool>(out var flag))
                {
                    return flag ? "true" : "false";
                }

                if (value.TryGetValue<DateTime>(out var date))
                {
                    return date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
                }

                return value.ToJsonString().Trim('"');
            }

            return node.ToJsonString();
        }
    }
}
