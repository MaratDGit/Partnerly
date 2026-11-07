using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Partnerly.Computer
{
    // Trigger filters of the Computer events (kept apart from the hub so tests can reach them).
    public static class ComputerEvents
    {
        public static bool MatchesStatus(JsonElement config, string state)
        {
            var wanted = Read(config, "state");
            return wanted.Length == 0 || wanted == "any" || string.Equals(wanted, state, StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesFile(JsonElement config, JsonObject payload)
        {
            var folder = Slashes(Read(config, "folder"));
            var actual = Slashes(payload["folder"]?.ToString() ?? string.Empty);
            if (folder.Length > 0 && !string.Equals(folder, actual, StringComparison.OrdinalIgnoreCase)
                && !actual.StartsWith(folder + (char)92, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var pattern = Read(config, "pattern");
            return pattern.Length == 0 || Wildcard(pattern, payload["fileName"]?.ToString() ?? string.Empty);
        }

        public static bool MatchesProgram(JsonElement config, JsonObject payload)
        {
            var wanted = StripExe(Read(config, "program"));
            return wanted.Length == 0 || string.Equals(wanted, StripExe(payload["name"]?.ToString() ?? string.Empty), StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesClipboard(JsonElement config, JsonObject payload)
        {
            var needle = Read(config, "textContains");
            return needle.Length == 0
                || (payload["text"]?.ToString() ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase);
        }

        // Git triggers: the repository folder (same folder written with / or \ is the same) and, optionally, the branch.
        public static bool MatchesRepo(JsonElement config, JsonObject payload, bool checkState)
        {
            var wanted = Slashes(Read(config, "repo"));
            var actual = Slashes(payload["repo"]?.ToString() ?? string.Empty);
            if (wanted.Length > 0 && !string.Equals(wanted, actual, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var branch = Read(config, "branch");
            if (branch.Length > 0 && !string.Equals(branch, payload["branch"]?.ToString() ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }

            return !checkState || MatchesStatus(config, payload["state"]?.ToString() ?? string.Empty);
        }

        // "E", "E:" and "E:/" all mean drive E.
        public static bool MatchesDrive(JsonElement config, JsonObject payload, bool checkState)
        {
            if (checkState && !MatchesStatus(config, payload["state"]?.ToString() ?? string.Empty))
            {
                return false;
            }

            var wanted = Read(config, "drive").Trim();
            var actual = (payload["drive"]?.ToString() ?? string.Empty).Trim();
            return wanted.Length == 0 || (actual.Length > 0 && char.ToUpperInvariant(wanted[0]) == char.ToUpperInvariant(actual[0]));
        }

        // "*.pdf;*.docx" - any of the masks may match; * and ? are the only wildcards.
        public static bool Wildcard(string patterns, string name)
        {
            foreach (var pattern in patterns.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                if (Regex.IsMatch(name, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                {
                    return true;
                }
            }

            return false;
        }

        // C:/Inbox and C:\Inbox\ are the same folder.
        private static string Slashes(string path) => path.Trim().Replace('/', (char)92).TrimEnd((char)92);

        private static string StripExe(string name) =>
            name.Trim().EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name.Trim()[..^4] : name.Trim();

        private static string Read(JsonElement config, string key) =>
            config.ValueKind == JsonValueKind.Object && config.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
    }
}
