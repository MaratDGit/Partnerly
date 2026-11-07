using System.Text.Json.Nodes;

namespace Partnerly.Agent.Security
{
    // Finds the Git repositories that lie inside the allowed folders, so the person only adds folders
    // and never has to list repositories by hand. Looks a few levels down and skips folders nobody
    // keeps repositories in (build output, caches, the Recycle Bin).
    public static class RepoFinder
    {
        private const int MaxFolders = 4000;

        private static readonly HashSet<string> Skip = new(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", "bin", "obj", "packages", "$RECYCLE.BIN", "System Volume Information", "Windows", "Program Files",
            "Program Files (x86)", "AppData", "__pycache__", "venv", ".venv", "target", "dist"
        };

        public static List<string> Find(IEnumerable<string> roots, int maxDepth = 3, int maxRepos = 50)
        {
            var found = new List<string>();
            var visited = 0;
            foreach (var rawRoot in roots)
            {
                if (string.IsNullOrWhiteSpace(rawRoot)) { continue; }
                string root;
                try { root = Path.GetFullPath(Environment.ExpandEnvironmentVariables(rawRoot.Trim())); }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { continue; }
                if (!Directory.Exists(root)) { continue; }

                var queue = new Queue<(string Path, int Depth)>();
                queue.Enqueue((root, 0));
                while (queue.Count > 0 && found.Count < maxRepos && visited < MaxFolders)
                {
                    var (folder, depth) = queue.Dequeue();
                    visited++;
                    if (IsRepository(folder))
                    {
                        if (!found.Contains(folder, StringComparer.OrdinalIgnoreCase)) { found.Add(folder); }
                        // A repository inside a repository (submodules, vendored code) is not listed again.
                        continue;
                    }

                    if (depth >= maxDepth) { continue; }
                    IEnumerable<string> children;
                    try { children = Directory.EnumerateDirectories(folder); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
                    foreach (var child in children)
                    {
                        var name = System.IO.Path.GetFileName(child);
                        if (name.StartsWith('.') || Skip.Contains(name)) { continue; }
                        try { if ((File.GetAttributes(child) & (FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System)) != 0) { continue; } }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
                        queue.Enqueue((child, depth + 1));
                    }
                }
            }

            return found;
        }

        // A normal repository has a ".git" folder; a worktree or submodule has a ".git" file.
        public static bool IsRepository(string folder)
        {
            try
            {
                var marker = System.IO.Path.Combine(folder, ".git");
                return Directory.Exists(marker) || File.Exists(marker);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return false;
            }
        }
    }

    // What the website's "Browse" window sees: the allowed folders, and inside them folders and files.
    // Everything goes through PathGuard, so nothing outside the allowed folders can be listed.
    public static class FolderBrowser
    {
        private const int MaxEntries = 300;

        public static JsonObject List(PathGuard guard, string? path)
        {
            var items = new JsonArray();
            if (string.IsNullOrWhiteSpace(path))
            {
                foreach (var root in guard.Roots)
                {
                    items.Add(Entry(System.IO.Path.GetFileName(root).Length > 0 ? root : root, root, true, Directory.Exists(root) && RepoFinder.IsRepository(root), null));
                }

                return new JsonObject { ["path"] = string.Empty, ["parent"] = null, ["isRoot"] = true, ["items"] = items, ["truncated"] = false };
            }

            var folder = guard.Resolve(path);
            if (!Directory.Exists(folder))
            {
                throw new AgentException("The folder was not found.");
            }

            string? parent = System.IO.Path.GetDirectoryName(folder);
            if (parent != null)
            {
                try { guard.Resolve(parent); } catch (AgentException) { parent = string.Empty; }
            }
            else
            {
                parent = string.Empty;
            }

            var truncated = false;
            var info = new DirectoryInfo(folder);
            var entries = new List<FileSystemInfo>();
            try
            {
                entries.AddRange(info.EnumerateDirectories().Where(Visible).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase));
                entries.AddRange(info.EnumerateFiles().Where(Visible).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase));
            }
            catch (UnauthorizedAccessException)
            {
                throw new AgentException("Windows does not let the program open this folder.");
            }

            foreach (var entry in entries)
            {
                if (items.Count >= MaxEntries) { truncated = true; break; }
                var isFolder = entry is DirectoryInfo;
                items.Add(Entry(entry.Name, entry.FullName, isFolder, isFolder && RepoFinder.IsRepository(entry.FullName), entry is FileInfo file ? file.Length : null));
            }

            return new JsonObject { ["path"] = folder, ["parent"] = parent, ["isRoot"] = false, ["isRepo"] = RepoFinder.IsRepository(folder), ["items"] = items, ["truncated"] = truncated };
        }

        private static bool Visible(FileSystemInfo entry) =>
            (entry.Attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) == 0;

        private static JsonObject Entry(string name, string path, bool isFolder, bool isRepo, long? size) => new()
        {
            ["name"] = name,
            ["path"] = path,
            ["isFolder"] = isFolder,
            ["isRepo"] = isRepo,
            ["size"] = size
        };
    }
}
