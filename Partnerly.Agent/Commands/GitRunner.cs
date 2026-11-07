using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Partnerly.Agent.Config;
using Partnerly.Agent.Security;

namespace Partnerly.Agent.Commands
{
    // Runs git on this PC for the site. The site sends a sub-command and its options as separate pieces
    // (never one text for a shell); GitGuard checks them, the repository must lie inside allowedRepos, and
    // git is started without a shell, without hooks, without a pager and without asking anything on the console.
    public sealed class GitRunner
    {
        private const int MaxOutputChars = 20000;
        private const int MaxErrorChars = 4000;

        private readonly AgentConfig _config;
        private readonly Func<string, bool> _confirm;
        private readonly PathGuard _repos;

        public GitRunner(AgentConfig config, Func<string, bool> confirm)
        {
            _config = config;
            _confirm = confirm;
            _repos = new PathGuard(config.AllowedFolders);
        }

        public PathGuard Repos => _repos;

        // ---------- the command of a card ----------
        public async Task<JsonObject> RunAsync(JsonObject args, CancellationToken cancellationToken)
        {
            var sub = (args["sub"]?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            var plan = GitGuard.Validate(sub, Strings(args["args"]), HostsAllowed());
            var repoText = args["repo"]?.ToString() ?? string.Empty;
            var timeout = TimeSpan.FromSeconds(int.TryParse(args["timeoutSeconds"]?.ToString(), out var seconds) && seconds > 0
                ? Math.Min(seconds, 900)
                : sub is "fetch" or "pull" or "push" or "clone" ? 300 : 60);
            var addMode = (args["add"]?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            var paths = Strings(args["paths"]);

            string cwd;
            var gitArgs = new List<string>();
            if (sub is "clone" or "init")
            {
                (cwd, gitArgs) = PrepareNewRepository(sub, plan.Args, repoText);
            }
            else
            {
                cwd = _repos.Resolve(repoText);
                if (!Directory.Exists(cwd))
                {
                    throw new AgentException("The repository folder was not found.");
                }

                await EnsureRepositoryAsync(cwd, cancellationToken);
                gitArgs.Add(sub);
                gitArgs.AddRange(plan.Args);
            }

            if (sub == "commit" && addMode.Length > 0)
            {
                CheckAddMode(addMode, paths);
            }

            Confirm(plan, cwd, addMode, args);

            // "Add everything and commit" is one card; git needs two commands for it.
            if (sub == "commit" && addMode.Length > 0)
            {
                var add = await ExecAsync(cwd, AddArguments(addMode, paths), timeout, readOnly: false, cancellationToken);
                if (add.ExitCode != 0)
                {
                    throw new AgentException("git add failed: " + Brief(add.Error.Length > 0 ? add.Error : add.Output));
                }
            }

            var run = await ExecAsync(cwd, WithQuietDiff(sub, gitArgs), timeout, !plan.Writes, cancellationToken);
            var result = new JsonObject
            {
                ["command"] = plan.Describe(),
                ["repo"] = cwd,
                ["exitCode"] = run.ExitCode,
                ["ok"] = run.ExitCode == 0,
                ["output"] = Cut(run.Output, MaxOutputChars),
                ["errorOutput"] = Cut(run.Error, MaxErrorChars),
                ["text"] = Cut((run.Output + (run.Error.Length > 0 ? "\n" + run.Error : string.Empty)).Trim(), 8000)
            };

            if (run.ExitCode != 0)
            {
                // "Nothing to commit" is an answer, not a failure.
                if (sub == "commit" && ((run.Output + run.Error).Contains("nothing to commit", StringComparison.OrdinalIgnoreCase)
                    || (run.Output + run.Error).Contains("nothing added to commit", StringComparison.OrdinalIgnoreCase)))
                {
                    result["ok"] = true;
                    result["committed"] = false;
                    return result;
                }

                throw new AgentException($"git {sub} failed (exit code {run.ExitCode}): " + Brief(run.Error.Length > 0 ? run.Error : run.Output));
            }

            if (sub == "commit")
            {
                var sha = await QuickAsync(cwd, new[] { "rev-parse", "HEAD" }, cancellationToken);
                var subject = await QuickAsync(cwd, new[] { "log", "-1", "--pretty=%s" }, cancellationToken);
                result["committed"] = true;
                result["sha"] = sha?.Trim();
                result["subject"] = subject?.Trim();
            }

            if (sub is "clone" or "init")
            {
                result["repo"] = _repos.Resolve(repoText);
            }

            return result;
        }

        // ---------- used by the watchers (no questions, read only) ----------
        public async Task<string?> QuickAsync(string repoPath, IReadOnlyList<string> gitArgs, CancellationToken cancellationToken)
        {
            try
            {
                var folder = _repos.Resolve(repoPath);
                if (!Directory.Exists(folder)) { return null; }
                var run = await ExecAsync(folder, gitArgs.ToList(), TimeSpan.FromSeconds(30), readOnly: true, cancellationToken);
                return run.ExitCode == 0 ? run.Output : null;
            }
            catch (AgentException)
            {
                return null;
            }
        }

        // ---------- checks ----------
        private IReadOnlyCollection<string> HostsAllowed() => _config.GitHosts;

        private void Confirm(GitPlan plan, string cwd, string addMode, JsonObject args)
        {
            var ask = (plan.Publishes && !_config.GitPushWithoutAsking)
                      || (plan.Risky && _config.GitAskForRisky)
                      || _config.ConfirmOps.Contains("git_" + plan.Sub, StringComparer.OrdinalIgnoreCase)
                      || _config.ConfirmOps.Contains("git", StringComparer.OrdinalIgnoreCase);
            if (!ask)
            {
                return;
            }

            var text = new StringBuilder("Partnerly wants to run in this folder:\n")
                .Append(cwd).Append("\n\n");
            if (addMode.Length > 0) { text.Append("git add (").Append(addMode).Append(")\n"); }
            var command = plan.Describe();
            text.Append(command.Length > 400 ? command[..400] + "..." : command).Append("\n\n");
            if (plan.Risky) { text.Append("This can lose work or rewrite history.\n\n"); }
            if (plan.Publishes) { text.Append("This sends your commits to another computer.\n\n"); }
            text.Append("Allow?");
            if (!_confirm(text.ToString()))
            {
                throw new AgentException("The person at this PC declined the command.");
            }
        }

        // The repository found by git must lie under an allowed folder: an allowed sub-folder of a bigger
        // repository would otherwise give access to the whole of it.
        private async Task EnsureRepositoryAsync(string cwd, CancellationToken cancellationToken)
        {
            var top = await ExecAsync(cwd, new List<string> { "rev-parse", "--show-toplevel" }, TimeSpan.FromSeconds(30), readOnly: true, cancellationToken);
            if (top.ExitCode != 0 || string.IsNullOrWhiteSpace(top.Output))
            {
                throw new AgentException("This folder is not a Git repository (or git said: " + Brief(top.Error) + ").");
            }

            try
            {
                _repos.Resolve(top.Output.Trim());
            }
            catch (AgentException)
            {
                throw new AgentException("The Git repository of this folder is bigger than the allowed folder. Allow the whole repository folder in Partnerly Agent (Folders).");
            }
        }

        private (string Cwd, List<string> Args) PrepareNewRepository(string sub, List<string> options, string repoText)
        {
            var destination = _repos.Resolve(repoText);
            var parent = Path.GetDirectoryName(destination) ?? throw new AgentException("The folder is not valid.");
            var args = new List<string> { sub };
            if (sub == "init")
            {
                // Only the name of the first branch may be chosen.
                for (var i = 0; i < options.Count; i++)
                {
                    var option = options[i];
                    if (option.StartsWith("--initial-branch=", StringComparison.Ordinal) && SafeName(option["--initial-branch=".Length..]))
                    {
                        args.Add(option);
                    }
                    else if (option == "-b" && i + 1 < options.Count && SafeName(options[i + 1]))
                    {
                        args.Add("--initial-branch=" + options[++i]);
                    }
                    else
                    {
                        throw new AgentException("Only --initial-branch can be used with git init here.");
                    }
                }

                Directory.CreateDirectory(destination);
                return (destination, args);
            }

            if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
            {
                throw new AgentException("The folder already exists and is not empty.");
            }

            var positional = options.Where(item => !item.StartsWith('-')).ToList();
            // Values of options that take one (--branch main, --depth 1) are not addresses.
            var url = options.Count > 0 ? options.Last(item => !item.StartsWith('-')) : string.Empty;
            if (string.IsNullOrEmpty(url) || positional.Count == 0)
            {
                throw new AgentException("Give the address of the repository to clone.");
            }

            // A plain name would be a folder on this PC; only a real address is cloned.
            if (!url.Contains("://") && !(url.Contains('@') && url.Contains(':')))
            {
                throw new AgentException("Give the full address of the repository, for example https://github.com/owner/name.git.");
            }

            for (var i = 0; i < options.Count; i++)
            {
                var item = options[i];
                if (item == url && i == options.LastIndexOf(url))
                {
                    continue;
                }

                args.Add(item);
            }

            Directory.CreateDirectory(parent);
            args.Add("--");
            args.Add(url);
            args.Add(Path.GetFileName(destination));
            return (parent, args);
        }

        private static bool SafeName(string name) =>
            name.Length is > 0 and <= 100 && name.All(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-' or '/');

        private static void CheckAddMode(string mode, List<string> paths)
        {
            if (mode is "all" or "tracked") { return; }
            if (mode != "paths" || paths.Count == 0)
            {
                throw new AgentException("Choose what to add: everything, changed files, or a list of files.");
            }

            foreach (var path in paths)
            {
                if (path.Length == 0 || path.StartsWith('-') || Path.IsPathRooted(path) || path.Contains("..") || path.Any(ch => ch == '\0' || ch == '\r' || ch == '\n'))
                {
                    throw new AgentException("Files to add must be paths inside the repository, for example docs/readme.md.");
                }
            }
        }

        private static List<string> AddArguments(string mode, List<string> paths)
        {
            var args = new List<string> { "add" };
            switch (mode)
            {
                case "tracked": args.Add("-u"); break;
                case "paths": args.Add("--"); args.AddRange(paths); break;
                default: args.Add("-A"); break;
            }

            return args;
        }

        // git must never start an external diff program or a text converter chosen by the repository settings.
        private static List<string> WithQuietDiff(string sub, List<string> args)
        {
            if (sub is "diff" or "log" or "show")
            {
                args.Insert(1, "--no-textconv");
                args.Insert(1, "--no-ext-diff");
            }

            return args;
        }

        // ---------- starting git ----------
        private sealed record GitRun(int ExitCode, string Output, string Error);

        private async Task<GitRun> ExecAsync(string cwd, List<string> gitArgs, TimeSpan timeout, bool readOnly, CancellationToken cancellationToken)
        {
            var exe = FindGit();
            var info = new ProcessStartInfo(exe)
            {
                WorkingDirectory = cwd,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var arg in BaseArguments()) { info.ArgumentList.Add(arg); }
            foreach (var arg in gitArgs) { info.ArgumentList.Add(arg); }

            // Nothing may wait for a person at a console: no password prompt, no editor, no pager.
            info.Environment["GIT_TERMINAL_PROMPT"] = "0";
            info.Environment["GCM_INTERACTIVE"] = "never";
            info.Environment["GIT_EDITOR"] = "true";
            info.Environment["GIT_SEQUENCE_EDITOR"] = "true";
            info.Environment["GIT_PAGER"] = "cat";
            info.Environment["PAGER"] = "cat";
            info.Environment["GIT_ALLOW_PROTOCOL"] = "https:http:ssh:git:file";
            if (readOnly) { info.Environment["GIT_OPTIONAL_LOCKS"] = "0"; }

            using var process = new Process { StartInfo = info };
            try
            {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                throw new AgentException("Git could not be started. Install Git for Windows (git-scm.com) or set gitPath in the Partnerly Agent settings.");
            }

            process.StandardInput.Close();
            var output = ReadCappedAsync(process.StandardOutput, MaxOutputChars + 1000);
            var error = ReadCappedAsync(process.StandardError, MaxErrorChars + 1000);
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limit.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(limit.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { /* already finished */ }
                cancellationToken.ThrowIfCancellationRequested();
                throw new AgentException($"git did not finish in {(int)timeout.TotalSeconds} seconds and was stopped.");
            }

            return new GitRun(process.ExitCode, await output, await error);
        }

        private IEnumerable<string> BaseArguments()
        {
            yield return "--no-pager";
            yield return "-c"; yield return "core.fsmonitor=false";
            yield return "-c"; yield return "core.quotepath=false";
            yield return "-c"; yield return "protocol.ext.allow=never";
            if (!_config.GitRunHooks)
            {
                // Hooks are programs stored in the repository; the site must not be able to start them.
                yield return "-c"; yield return "core.hooksPath=" + NoHooksFolder();
            }
        }

        private static string NoHooksFolder()
        {
            var folder = Path.Combine(AgentConfig.Folder, "nohooks");
            try { Directory.CreateDirectory(folder); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* git then finds no hooks anyway */ }
            return folder;
        }

        private string FindGit()
        {
            if (!string.IsNullOrWhiteSpace(_config.GitPath))
            {
                var configured = Environment.ExpandEnvironmentVariables(_config.GitPath.Trim());
                if (File.Exists(configured)) { return configured; }
            }

            var candidates = new List<string>();
            foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                candidates.Add(Path.Combine(folder.Trim('"'), "git.exe"));
            }

            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "cmd", "git.exe"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git", "cmd", "git.exe"));
            foreach (var candidate in candidates)
            {
                try { if (File.Exists(candidate)) { return candidate; } }
                catch (ArgumentException) { /* a bad entry in PATH */ }
            }

            throw new AgentException("Git is not installed on this PC. Install Git for Windows (git-scm.com), then try again.");
        }

        // ---------- small helpers ----------
        private static async Task<string> ReadCappedAsync(StreamReader reader, int cap)
        {
            var builder = new StringBuilder();
            var buffer = new char[4096];
            int read;
            while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                // Keep reading so git never blocks on a full pipe, but keep only the start.
                if (builder.Length < cap)
                {
                    builder.Append(buffer, 0, Math.Min(read, cap - builder.Length));
                }
            }

            return builder.ToString();
        }

        private static List<string> Strings(JsonNode? node) =>
            node is JsonArray array
                ? array.Select(item => item?.ToString() ?? string.Empty).ToList()
                : new List<string>();

        private static string Cut(string text, int max) => text.Length > max ? text[..max] + "\n... (cut)" : text;

        private static string Brief(string text)
        {
            var line = text.Trim().Replace("\r", string.Empty);
            return line.Length > 600 ? line[..600] + "..." : line;
        }
    }
}
