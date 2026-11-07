using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Integrations.Common;

namespace Partnerly.Computer
{
    // Builds the sub-command and options of one git card. The PC checks everything again (GitGuard),
    // this only keeps obviously wrong input (a value that looks like an option) from being sent.
    public sealed class GitRequest
    {
        private readonly ActionContext _context;
        private readonly IVariableBinder _binder;

        public GitRequest(ActionContext context)
        {
            _context = context;
            _binder = context.Services.GetRequiredService<IVariableBinder>();
        }

        public string Sub { get; set; } = string.Empty;
        public List<string> Args { get; } = new();
        public string Add { get; set; } = string.Empty;
        public List<string> Paths { get; } = new();

        public string Text(string key) => _binder.Interpolate(NodeConfig.GetString(_context.Node, key), _context.Variables).Trim();

        public bool Flag(string key) => string.Equals(Text(key), "true", StringComparison.OrdinalIgnoreCase);

        public int Int(string key, int fallback, int min, int max) =>
            int.TryParse(Text(key), out var value) ? Math.Clamp(value, min, max) : fallback;

        // A name, a branch, an address: it must never be taken for an option.
        public string Value(string key, string label, bool required = false)
        {
            var value = Text(key);
            if (value.Length == 0)
            {
                return required ? throw new GitCardException(label + " is required.") : string.Empty;
            }

            if (value.StartsWith('-'))
            {
                throw new GitCardException(label + " must not start with a dash.");
            }

            return value;
        }

        public void AddValue(string key, string label, bool required = false)
        {
            var value = Value(key, label, required);
            if (value.Length > 0) { Args.Add(value); }
        }

        public List<string> List(string key) =>
            Text(key).Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    public sealed class GitCardException : Exception
    {
        public GitCardException(string message) : base(message) { }
    }

    public abstract class ComputerGitActionBase : ComputerActionBase
    {
        protected ComputerGitActionBase(IComputerGateway gateway) : base(gateway) { }

        protected override string Op => "git";
        protected override string[] Fields => new[] { "repo" };

        // Network commands may take a while.
        protected override TimeSpan TimeoutFor(JsonObject args) =>
            args["sub"]?.ToString() is "fetch" or "pull" or "push" or "clone" or "ls-remote"
                ? TimeSpan.FromSeconds(330)
                : TimeSpan.FromSeconds(90);

        protected abstract void Build(GitRequest request);

        protected override Task<string?> PrepareAsync(ActionContext context, JsonObject args, CancellationToken cancellationToken)
        {
            var request = new GitRequest(context);
            try
            {
                Build(request);
            }
            catch (GitCardException ex)
            {
                return Task.FromResult<string?>(ex.Message);
            }

            args["sub"] = request.Sub;
            args["args"] = new JsonArray(request.Args.Select(item => (JsonNode?)JsonValue.Create(item)).ToArray());
            if (request.Add.Length > 0)
            {
                args["add"] = request.Add;
                args["paths"] = new JsonArray(request.Paths.Select(item => (JsonNode?)JsonValue.Create(item)).ToArray());
            }

            return Task.FromResult<string?>(null);
        }

        protected override Task<string?> FinishAsync(ActionContext context, JsonObject data, CancellationToken cancellationToken)
        {
            Shape(data);
            return Task.FromResult<string?>(null);
        }

        protected virtual void Shape(JsonObject data) { }

        protected static void Require(GitRequest request)
        {
            if (request.Text("repo").Length == 0)
            {
                throw new GitCardException("Repository folder is required.");
            }
        }
    }

    // ---------- reading ----------
    public sealed class ComputerGitStatusAction : ComputerGitActionBase
    {
        public ComputerGitStatusAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitStatus;
        public override string DisplayName => "Git status";
        protected override bool Writes => false;

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "status";
            r.Args.Add("--porcelain=v1");
            r.Args.Add("-b");
        }

        protected override void Shape(JsonObject data) => GitOutput.ShapeStatus(data);
    }

    public sealed class ComputerGitLogAction : ComputerGitActionBase
    {
        public ComputerGitLogAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitLog;
        public override string DisplayName => "Git log";
        protected override bool Writes => false;

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "log";
            r.Args.Add("-n");
            r.Args.Add(r.Int("max", 10, 1, 200).ToString());
            r.Args.Add(GitOutput.LogFormat);
            r.AddValue("branch", "Branch");
            var path = r.Value("path", "Path");
            if (path.Length > 0)
            {
                r.Args.Add("--");
                r.Args.Add(path);
            }
        }

        protected override void Shape(JsonObject data) => GitOutput.ShapeLog(data);
    }

    public sealed class ComputerGitDiffAction : ComputerGitActionBase
    {
        public ComputerGitDiffAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitDiff;
        public override string DisplayName => "Git diff";
        protected override bool Writes => false;

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "diff";
            var mode = r.Text("mode");
            r.Args.Add(mode == "names" ? "--name-status" : mode == "full" ? "--no-color" : "--stat");
            if (r.Flag("staged")) { r.Args.Add("--staged"); }
            r.AddValue("target", "Compare with");
            var path = r.Value("path", "Path");
            if (path.Length > 0)
            {
                r.Args.Add("--");
                r.Args.Add(path);
            }
        }

        protected override void Shape(JsonObject data)
        {
            var text = data["output"]?.ToString() ?? string.Empty;
            data["changed"] = text.Trim().Length > 0;
            data["lines"] = text.Length == 0 ? 0 : text.Split('\n').Length;
        }
    }

    public sealed class ComputerGitBranchesAction : ComputerGitActionBase
    {
        public ComputerGitBranchesAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitBranches;
        public override string DisplayName => "Git branches";
        protected override bool Writes => false;

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "branch";
            r.Args.Add("--list");
            if (r.Flag("remote")) { r.Args.Add("-a"); }
            r.Args.Add(GitOutput.BranchFormat);
        }

        protected override void Shape(JsonObject data) => GitOutput.ShapeBranches(data);
    }

    // ---------- changing ----------
    public sealed class ComputerGitFetchAction : ComputerGitActionBase
    {
        public ComputerGitFetchAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitFetch;
        public override string DisplayName => "Git fetch";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "fetch";
            if (r.Flag("prune")) { r.Args.Add("--prune"); }
            r.AddValue("remote", "Remote");
        }
    }

    public sealed class ComputerGitPullAction : ComputerGitActionBase
    {
        public ComputerGitPullAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitPull;
        public override string DisplayName => "Git pull";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "pull";
            var mode = r.Text("mode");
            r.Args.Add(mode == "merge" ? "--no-rebase" : mode == "rebase" ? "--rebase" : "--ff-only");
            r.AddValue("remote", "Remote");
            r.AddValue("branch", "Branch");
        }

        protected override void Shape(JsonObject data)
        {
            var text = data["output"]?.ToString() ?? string.Empty;
            data["upToDate"] = text.Contains("Already up to date", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class ComputerGitCommitAction : ComputerGitActionBase
    {
        public ComputerGitCommitAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitCommit;
        public override string DisplayName => "Git commit";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "commit";
            var message = r.Text("message");
            if (message.Length == 0) { throw new GitCardException("Commit message is required."); }
            r.Args.Add("-m");
            r.Args.Add(message);
            switch (r.Text("add"))
            {
                case "none":
                    break;
                case "tracked":
                    r.Add = "tracked";
                    break;
                case "paths":
                    r.Add = "paths";
                    r.Paths.AddRange(r.List("paths"));
                    if (r.Paths.Count == 0) { throw new GitCardException("List the files to commit."); }
                    break;
                default:
                    r.Add = "all";
                    break;
            }
        }
    }

    public sealed class ComputerGitPushAction : ComputerGitActionBase
    {
        public ComputerGitPushAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitPush;
        public override string DisplayName => "Git push";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "push";
            if (r.Flag("setUpstream")) { r.Args.Add("-u"); }
            if (r.Flag("tags")) { r.Args.Add("--tags"); }
            switch (r.Text("force"))
            {
                case "lease": r.Args.Add("--force-with-lease"); break;
                case "force": r.Args.Add("--force"); break;
            }

            var remote = r.Value("remote", "Remote");
            var branch = r.Value("branch", "Branch");
            if (remote.Length == 0 && branch.Length > 0) { remote = "origin"; }
            if (remote.Length > 0) { r.Args.Add(remote); }
            if (branch.Length > 0) { r.Args.Add(branch); }
        }
    }

    public sealed class ComputerGitSwitchAction : ComputerGitActionBase
    {
        public ComputerGitSwitchAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitSwitch;
        public override string DisplayName => "Git switch branch";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "switch";
            if (r.Flag("create")) { r.Args.Add("-c"); }
            r.AddValue("branch", "Branch", required: true);
            if (r.Flag("create")) { r.AddValue("from", "Start from"); }
        }
    }

    public sealed class ComputerGitCloneAction : ComputerGitActionBase
    {
        public ComputerGitCloneAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitClone;
        public override string DisplayName => "Git clone";

        protected override void Build(GitRequest r)
        {
            r.Sub = "clone";
            if (r.Text("repo").Length == 0) { throw new GitCardException("Folder for the copy is required."); }
            var branch = r.Value("branch", "Branch");
            if (branch.Length > 0) { r.Args.Add("--branch"); r.Args.Add(branch); }
            var depth = r.Int("depth", 0, 0, 100000);
            if (depth > 0) { r.Args.Add("--depth"); r.Args.Add(depth.ToString()); }
            r.AddValue("url", "Repository address", required: true);
        }
    }

    public sealed class ComputerGitInitAction : ComputerGitActionBase
    {
        public ComputerGitInitAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitInit;
        public override string DisplayName => "Git init";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "init";
            var branch = r.Value("branch", "First branch");
            if (branch.Length > 0) { r.Args.Add("--initial-branch=" + branch); }
        }
    }

    public sealed class ComputerGitMergeAction : ComputerGitActionBase
    {
        public ComputerGitMergeAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitMerge;
        public override string DisplayName => "Git merge";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "merge";
            if (r.Flag("noFastForward")) { r.Args.Add("--no-ff"); }
            var message = r.Text("message");
            if (message.Length > 0) { r.Args.Add("-m"); r.Args.Add(message); }
            r.AddValue("branch", "Branch to merge", required: true);
        }
    }

    public sealed class ComputerGitTagAction : ComputerGitActionBase
    {
        public ComputerGitTagAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitTag;
        public override string DisplayName => "Git tag";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "tag";
            if (r.Flag("delete")) { r.Args.Add("-d"); r.AddValue("name", "Tag", required: true); return; }
            var message = r.Text("message");
            if (message.Length > 0) { r.Args.Add("-a"); r.Args.Add("-m"); r.Args.Add(message); }
            r.AddValue("name", "Tag", required: true);
            r.AddValue("target", "Commit");
        }
    }

    public sealed class ComputerGitStashAction : ComputerGitActionBase
    {
        public ComputerGitStashAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitStash;
        public override string DisplayName => "Git stash";

        protected override void Build(GitRequest r)
        {
            Require(r);
            r.Sub = "stash";
            var action = r.Text("action");
            switch (action)
            {
                case "pop": r.Args.Add("pop"); break;
                case "apply": r.Args.Add("apply"); break;
                case "list": r.Args.Add("list"); break;
                case "drop": r.Args.Add("drop"); break;
                default:
                    r.Args.Add("push");
                    r.Args.Add("-u");
                    var message = r.Text("message");
                    if (message.Length > 0) { r.Args.Add("-m"); r.Args.Add(message); }
                    break;
            }
        }
    }

    // Any other git command, typed as in a terminal ("reset --soft HEAD~1", "cherry-pick abc123", "remote -v").
    // The PC still allows only its list of sub-commands and refuses options that run programs.
    public sealed class ComputerGitCommandAction : ComputerGitActionBase
    {
        public ComputerGitCommandAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GitCommand;
        public override string DisplayName => "Git command";

        protected override void Build(GitRequest r)
        {
            Require(r);
            var tokens = GitOutput.Tokenize(r.Text("command"));
            if (tokens.Count > 0 && string.Equals(tokens[0], "git", StringComparison.OrdinalIgnoreCase)) { tokens.RemoveAt(0); }
            if (tokens.Count == 0) { throw new GitCardException("Type the git command, for example: log --oneline -5"); }
            r.Sub = tokens[0].ToLowerInvariant();
            r.Args.AddRange(tokens.Skip(1));
        }
    }

    // Turns the text git prints into small values for the next cards.
    public static class GitOutput
    {
        public const string LogFormat = "--pretty=format:%H%x1f%an%x1f%aI%x1f%s";
        public const string BranchFormat = "--format=%(HEAD)|%(refname:short)";

        // Splits a typed command into words; "double" and 'single' quotes keep spaces together.
        public static List<string> Tokenize(string text)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            char quote = '\0';
            var started = false;
            foreach (var ch in text)
            {
                if (quote != '\0')
                {
                    if (ch == quote) { quote = '\0'; } else { current.Append(ch); }
                }
                else if (ch is '"' or '\'')
                {
                    quote = ch;
                    started = true;
                }
                else if (char.IsWhiteSpace(ch))
                {
                    if (started || current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        started = false;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (started || current.Length > 0) { tokens.Add(current.ToString()); }
            return tokens;
        }

        public static void ShapeStatus(JsonObject data)
        {
            var lines = (data["output"]?.ToString() ?? string.Empty).Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var branch = string.Empty;
            var upstream = string.Empty;
            var ahead = 0;
            var behind = 0;
            var staged = 0;
            var unstaged = 0;
            var untracked = 0;
            var files = new JsonArray();
            foreach (var line in lines)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    var head = line[3..];
                    var bracket = head.IndexOf(" [", StringComparison.Ordinal);
                    var names = bracket >= 0 ? head[..bracket] : head;
                    var dots = names.IndexOf("...", StringComparison.Ordinal);
                    branch = dots >= 0 ? names[..dots] : names;
                    upstream = dots >= 0 ? names[(dots + 3)..] : string.Empty;
                    if (branch.StartsWith("No commits yet on ", StringComparison.Ordinal)) { branch = branch["No commits yet on ".Length..]; }
                    if (bracket >= 0)
                    {
                        foreach (var part in head[(bracket + 2)..].TrimEnd(']').Split(',', StringSplitOptions.TrimEntries))
                        {
                            if (part.StartsWith("ahead ", StringComparison.Ordinal)) { int.TryParse(part[6..], out ahead); }
                            if (part.StartsWith("behind ", StringComparison.Ordinal)) { int.TryParse(part[7..], out behind); }
                        }
                    }

                    continue;
                }

                if (line.Length < 4) { continue; }
                var x = line[0];
                var y = line[1];
                if (x == '?' && y == '?') { untracked++; }
                else
                {
                    if (x != ' ') { staged++; }
                    if (y != ' ') { unstaged++; }
                }

                if (files.Count < 50)
                {
                    files.Add(new JsonObject { ["status"] = line[..2].Trim(), ["path"] = line[3..] });
                }
            }

            var changes = lines.Count(l => !l.StartsWith("## ", StringComparison.Ordinal));
            data["branch"] = branch;
            data["upstream"] = upstream;
            data["ahead"] = ahead;
            data["behind"] = behind;
            data["changes"] = changes;
            data["staged"] = staged;
            data["unstaged"] = unstaged;
            data["untracked"] = untracked;
            data["clean"] = changes == 0;
            data["files"] = files;
            data["summary"] = changes == 0
                ? $"{branch}: clean"
                : $"{branch}: {changes} change(s)" + (ahead > 0 ? $", {ahead} to push" : string.Empty) + (behind > 0 ? $", {behind} to pull" : string.Empty);
        }

        public static void ShapeLog(JsonObject data)
        {
            var commits = new JsonArray();
            foreach (var line in (data["output"]?.ToString() ?? string.Empty).Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('\u001f');
                if (parts.Length < 4) { continue; }
                commits.Add(new JsonObject
                {
                    ["sha"] = parts[0],
                    ["shortSha"] = parts[0].Length > 7 ? parts[0][..7] : parts[0],
                    ["author"] = parts[1],
                    ["date"] = parts[2],
                    ["subject"] = string.Join("\u001f", parts.Skip(3))
                });
            }

            data["count"] = commits.Count;
            data["first"] = commits.Count > 0 ? JsonNodes.Clone(commits[0]!) : null;
            data["commits"] = commits;
        }

        public static void ShapeBranches(JsonObject data)
        {
            var names = new JsonArray();
            var current = string.Empty;
            foreach (var line in (data["output"]?.ToString() ?? string.Empty).Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var bar = line.IndexOf('|');
                if (bar < 0) { continue; }
                var name = line[(bar + 1)..].Trim();
                if (name.Length == 0 || name.StartsWith("(HEAD detached", StringComparison.Ordinal)) { continue; }
                if (line[0] == '*') { current = name; }
                names.Add(name);
            }

            data["count"] = names.Count;
            data["current"] = current;
            data["names"] = string.Join(", ", names.Select(item => item!.ToString()));
            data["branches"] = names;
        }
    }
}
