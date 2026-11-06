namespace Partnerly.Agent.Security
{
    // Decides which places on this PC the site may touch. Only folders listed in the agent
    // settings (and everything below them) are allowed; everything else is refused here,
    // whatever the site asks for.
    public sealed class PathGuard
    {
        private readonly List<string> _roots;

        public PathGuard(IEnumerable<string> roots)
        {
            _roots = roots
                .Where(root => !string.IsNullOrWhiteSpace(root))
                .Select(root => Normalize(Environment.ExpandEnvironmentVariables(root.Trim())))
                .Where(root => root.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<string> Roots => _roots;

        // Full path of an allowed place, or an AgentException.
        public string Resolve(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new AgentException("The path is empty.");
            }

            var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
            if (!Path.IsPathRooted(expanded))
            {
                throw new AgentException("Use a full path that starts with a drive letter, for example C:/Users/me/Documents/file.txt.");
            }

            // Device paths start with two backslashes and a ? or a dot (built from char codes on purpose).
            var slashes = new string((char)92, 2);
            if (expanded.StartsWith(slashes + "?", StringComparison.Ordinal) || expanded.StartsWith(slashes + ".", StringComparison.Ordinal))
            {
                throw new AgentException("Device paths are not allowed.");
            }

            var full = Normalize(expanded);
            if (full.IndexOf(':', 2) >= 0)
            {
                throw new AgentException("Alternate data streams are not allowed.");
            }

            foreach (var root in _roots)
            {
                if (!IsUnder(full, root))
                {
                    continue;
                }

                RejectLinks(full, root);
                return full;
            }

            throw new AgentException("This place is not allowed in the Partnerly Agent settings (allowedFolders).");
        }

        // A drive root such as "F:\" already ends with a separator: adding one more made every file on
        // the drive "outside" it, so allowing a whole drive did not work.
        public static bool IsUnder(string full, string root)
        {
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var prefix = root.EndsWith(Path.DirectorySeparatorChar) || root.EndsWith(Path.AltDirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;
            return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // A shortcut/junction inside an allowed folder could lead outside of it.
        private static void RejectLinks(string full, string root)
        {
            var current = full;
            while (current.Length > root.Length && IsUnder(current, root))
            {
                if ((File.Exists(current) || Directory.Exists(current))
                    && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new AgentException("Links and junctions are not followed.");
                }

                var parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || parent.Length >= current.Length)
                {
                    break;
                }

                current = parent;
            }
        }

        private static string Normalize(string path)
        {
            try
            {
                var full = Path.GetFullPath(path);
                var rootOfPath = Path.GetPathRoot(full) ?? string.Empty;
                return full.Length > rootOfPath.Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new AgentException("The path is not valid.");
            }
        }
    }
}
