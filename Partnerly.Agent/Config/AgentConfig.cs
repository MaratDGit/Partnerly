using System.Text.Json;
using Partnerly.Agent.Security;

namespace Partnerly.Agent.Config
{
    // Everything this PC allows is written here, in a file that only this PC owns.
    // The site can ask for anything, but it only gets what these lists permit.
    public sealed class AgentConfig
    {
        public string Server { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        // The master switch: off = the program stays in the tray but is not connected to the site.
        public bool Enabled { get; set; } = true;

        // Files and folders the site may read, write, copy, zip and open (with everything below them).
        public List<string> AllowedFolders { get; set; } = new();

        // Programs the site may start or close: a full path, or a bare name such as notepad.exe.
        public List<string> AllowedPrograms { get; set; } = new();

        // Old settings, kept only so an old file still loads: folders for scripts, Git and watching now
        // all mean "the allowed folders" and are merged into AllowedFolders when the file is read.
        public List<string> ScriptFolders { get; set; } = new();

        // Old setting (see ScriptFolders). Git works in every repository inside the allowed folders.
        public List<string> AllowedRepos { get; set; } = new();

        // 2 = folders, scripts, Git and watching are one list.
        public int SettingsVersion { get; set; }

        // Hosts a repository address may point to (clone, remote, push by address). Empty = any host (https, ssh, git only).
        public List<string> GitHosts { get; set; } = new();

        // Full path of git.exe when it is not found by itself.
        public string GitPath { get; set; } = string.Empty;

        // Off by default: hooks are programs stored in a repository and would run with the site's commands.
        public bool GitRunHooks { get; set; }

        // Commands that can lose work or rewrite history (reset --hard, push --force, rebase, clean...) ask first.
        public bool GitAskForRisky { get; set; } = true;

        // Pushing sends your commits away, so it asks first unless you switch this on.
        public bool GitPushWithoutAsking { get; set; }

        // Old setting: repositories are found by themselves inside the allowed folders.
        public List<string> WatchRepos { get; set; } = new();

        // Old setting: every allowed folder is watched.
        public List<string> WatchFolders { get; set; } = new();
        public bool WatchSubfolders { get; set; }

        // Old setting: every allowed program is watched.
        public List<string> WatchPrograms { get; set; } = new();

        // Off by default: the text you copy can be private.
        public bool WatchClipboard { get; set; }

        // "Nobody touches the PC" starts after this many minutes; 0 turns the idle events off.
        public int IdleMinutes { get; set; } = 10;

        // A drive with less free space than this (GB) is reported; 0 turns it off.
        public int DiskLowGb { get; set; } = 5;

        // Commands that ask for a click on this PC before they run, e.g. "run_program", "lock".
        public List<string> ConfirmOps { get; set; } = new() { "shutdown", "delete_file" };

        // The welcome window was shown once.
        public bool Welcomed { get; set; }

        // PARTNERLY_AGENT_HOME moves settings and log elsewhere (a portable copy, tests).
        public static string Folder =>
            Environment.GetEnvironmentVariable("PARTNERLY_AGENT_HOME") is { Length: > 0 } home
                ? home
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PartnerlyAgent");

        public static string FilePath => Path.Combine(Folder, "config.json");

        public static string LogPath => Path.Combine(Folder, "agent.log");

        // Set when the settings file exists but could not be read (damaged); the program says so.
        public static string? LoadProblem { get; private set; }

        public static AgentConfig? Load()
        {
            LoadProblem = null;
            var main = TryRead(FilePath, out var problem);
            if (main != null)
            {
                main.Normalize();
                return main;
            }

            if (problem != null)
            {
                // The file is damaged: try the copy made at the last good save.
                var backup = TryRead(FilePath + ".bak", out _);
                if (backup != null)
                {
                    try { File.Copy(FilePath, FilePath + ".damaged", true); } catch (IOException) { /* keep going */ }
                    LoadProblem = "restored";
                    backup.Normalize();
                    return backup;
                }

                LoadProblem = problem;
            }

            return null;
        }

        private static AgentConfig? TryRead(string path, out string? problem)
        {
            problem = null;
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var config = JsonSerializer.Deserialize<AgentConfig>(File.ReadAllText(path), Options);
                if (config == null)
                {
                    problem = "empty";
                }

                return config;
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                problem = ex.Message;
                return null;
            }
        }

        // One list of folders is enough: Partnerly may read, write, watch and use Git in them and below them.
        // An old settings file is merged once (SettingsVersion), so nothing that was allowed before is lost.
        public void Normalize()
        {
            if (SettingsVersion >= 2)
            {
                return;
            }

            static IEnumerable<string> Clean(IEnumerable<string>? items) =>
                (items ?? Enumerable.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim());

            var hadScriptFolders = ScriptFolders.Count > 0;
            var folders = new List<string>(Clean(AllowedFolders));
            foreach (var item in Clean(AllowedRepos).Concat(Clean(ScriptFolders)).Concat(Clean(WatchFolders)))
            {
                if (!folders.Contains(item, StringComparer.OrdinalIgnoreCase)) { folders.Add(item); }
            }

            var programs = new List<string>(Clean(AllowedPrograms));
            foreach (var item in Clean(WatchPrograms))
            {
                if (!programs.Any(existing => string.Equals(ProgramName(existing), ProgramName(item), StringComparison.OrdinalIgnoreCase))) { programs.Add(item); }
            }

            AllowedFolders = folders;
            AllowedPrograms = programs;
            ScriptFolders = new List<string>();
            AllowedRepos = new List<string>();
            WatchFolders = new List<string>();
            WatchPrograms = new List<string>();
            WatchRepos = new List<string>();

            // Scripts used to need their own folder; now they can run from any allowed folder, so they ask first
            // unless the person had set scripts up on purpose.
            if (!hadScriptFolders && !ConfirmOps.Contains("run_script", StringComparer.OrdinalIgnoreCase))
            {
                ConfirmOps = new List<string>(ConfirmOps) { "run_script" };
            }

            SettingsVersion = 2;
        }

        // "C:\Tools\App.exe", "App.exe" and "app" are the same program name.
        public static string ProgramName(string value)
        {
            var name = value.Trim().Trim('"');
            var slash = Math.Max(name.LastIndexOf('\\'), name.LastIndexOf('/'));
            if (slash >= 0) { name = name[(slash + 1)..]; }
            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        }

        // Written to a temporary file first, so a power cut can never leave half a file behind.
        public void Save()
        {
            Directory.CreateDirectory(Folder);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this, Options));
            if (File.Exists(FilePath))
            {
                File.Copy(FilePath, FilePath + ".bak", true);
                File.Replace(temp, FilePath, null);
            }
            else
            {
                File.Move(temp, FilePath);
            }
        }

        public static AgentConfig CreateDefault(string server, string token)
        {
            var documents = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Partnerly");
            try { Directory.CreateDirectory(documents); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* the list still names it */ }
            return new AgentConfig
            {
                Server = server.Trim().TrimEnd('/'),
                Token = token.Trim(),
                AllowedFolders = new List<string> { documents },
                ConfirmOps = new List<string> { "shutdown", "delete_file", "run_script" },
                SettingsVersion = 2
            };
        }

        // What the site may know: only the lists, never the token.
        public string ToSiteJson() => JsonSerializer.Serialize(new
        {
            folders = AllowedFolders,
            programs = AllowedPrograms,
            scripts = AllowedFolders,
            repos = RepoFinder.Find(AllowedFolders).Take(60).ToList(),
            confirm = ConfirmOps
        }, Options);

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
