using System.Text.Json;

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

        // Folders that may hold .ps1 / .bat / .cmd scripts the site can run.
        public List<string> ScriptFolders { get; set; } = new();

        // Folders where new / changed / deleted files are reported to Partnerly.
        public List<string> WatchFolders { get; set; } = new();
        public bool WatchSubfolders { get; set; }

        // Program names reported when they start or stop.
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
                AllowedFolders = new List<string> { documents }
            };
        }

        // What the site may know: only the lists, never the token.
        public string ToSiteJson() => JsonSerializer.Serialize(new
        {
            folders = AllowedFolders,
            programs = AllowedPrograms,
            scripts = ScriptFolders,
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
