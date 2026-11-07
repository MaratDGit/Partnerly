namespace Partnerly.Agent.Security
{
    // What the site asked git to do, after the checks. Nothing here talks to git yet.
    public sealed class GitPlan
    {
        public string Sub { get; init; } = string.Empty;
        public List<string> Args { get; init; } = new();

        // Changes files, history or a remote.
        public bool Writes { get; init; }

        // Can lose work or rewrite history: the person at this PC is asked first (unless switched off).
        public bool Risky { get; init; }

        // Sends data to another computer (push) or changes where data goes (remote add / set-url).
        public bool Publishes { get; init; }

        public string Describe() => "git " + Sub + (Args.Count > 0 ? " " + string.Join(" ", Args) : string.Empty);
    }

    // Decides which git commands the site may run on this PC. The site's text never becomes a command line:
    // the sub-command must be on a list, every option is checked, and options that run programs, read or
    // write files outside the repository, or change git's own settings are refused.
    public static class GitGuard
    {
        private const int MaxArgs = 60;
        private const int MaxArgLength = 4000;

        private static readonly HashSet<string> ReadOnly = new(StringComparer.Ordinal)
        {
            "status", "log", "diff", "show", "rev-parse", "describe", "shortlog", "blame", "ls-files", "ls-tree", "ls-remote",
            "rev-list", "merge-base", "name-rev", "grep", "count-objects", "var"
        };

        private static readonly HashSet<string> Changing = new(StringComparer.Ordinal)
        {
            "add", "commit", "fetch", "pull", "push", "switch", "checkout", "merge", "stash", "tag", "branch", "restore", "reset",
            "rebase", "clean", "cherry-pick", "revert", "rm", "mv", "remote", "init", "clone"
        };

        // Sub-commands that run programs or change git's own settings are never offered:
        // config, submodule, filter-branch, daemon, credential, difftool, mergetool, gc, am, apply, bisect, archive...

        // Options refused in every command (name before "=").
        private static readonly HashSet<string> Forbidden = new(StringComparer.Ordinal)
        {
            "-C", "--config", "--config-env", "--git-dir", "--work-tree", "--exec-path", "--namespace", "--super-prefix",
            "--upload-pack", "--receive-pack", "--exec", "--ext-diff", "--textconv", "--output", "--open-files-in-pager",
            "--paginate", "--no-index", "--template", "--separate-git-dir", "--reference", "--reference-if-able", "--dissociate",
            "--recurse-submodules", "--recursive", "--shallow-file", "--file", "-F", "--ssh-command", "--exec-cmd", "--mirror-all",
            "--contents", "--help", "--html-path", "--man-path", "--info-path", "--attr-source"
        };

        private static readonly string[] AllowedUrlSchemes = { "https", "ssh", "git" };

        public static GitPlan Validate(string? sub, IReadOnlyList<string>? args, IReadOnlyCollection<string> allowedHosts)
        {
            sub = (sub ?? string.Empty).Trim().ToLowerInvariant();
            if (sub.Length == 0)
            {
                throw new AgentException("The git command is empty.");
            }

            var known = ReadOnly.Contains(sub) || Changing.Contains(sub);
            if (!known)
            {
                throw new AgentException($"The git command \"{Shorten(sub)}\" is not allowed here. Allowed: " + string.Join(", ", ReadOnly.Concat(Changing).OrderBy(x => x)) + ".");
            }

            var list = (args ?? Array.Empty<string>()).ToList();
            if (list.Count > MaxArgs)
            {
                throw new AgentException("Too many arguments for git.");
            }

            var urlLike = sub is "clone" or "fetch" or "pull" or "push" or "remote" or "ls-remote";
            foreach (var arg in list)
            {
                CheckText(arg);
                if (arg.StartsWith('-'))
                {
                    CheckOption(sub, arg);
                }
                else if (urlLike)
                {
                    CheckRemoteText(arg, allowedHosts);
                }
            }

            if (sub is "commit" or "tag" or "merge")
            {
                // Messages come with -m; a message file would read a file outside the repository.
                if (list.Any(a => a is "-F" or "-t" || a.StartsWith("--file", StringComparison.Ordinal) || a.StartsWith("--template", StringComparison.Ordinal)))
                {
                    throw new AgentException("Use -m for the message; message files are not allowed.");
                }
            }

            return new GitPlan
            {
                Sub = sub,
                Args = list,
                Writes = Changing.Contains(sub),
                Risky = IsRisky(sub, list),
                Publishes = Publishes(sub, list)
            };
        }

        // Words after the sub-command that make a command able to lose work or rewrite history.
        public static bool IsRisky(string sub, IReadOnlyList<string> args)
        {
            bool Has(params string[] names) => args.Any(a => names.Contains(a, StringComparer.Ordinal) || names.Any(n => n.StartsWith("--", StringComparison.Ordinal) && a.StartsWith(n + "=", StringComparison.Ordinal)));
            bool Short(char flag) => args.Any(a => a.Length > 1 && a[0] == '-' && a[1] != '-' && a.Contains(flag));
            switch (sub)
            {
                case "push":
                    return Has("--force", "--force-with-lease", "--delete", "--mirror", "--prune", "--force-if-includes") || Short('f') || Short('d')
                           || args.Any(a => !a.StartsWith('-') && (a.StartsWith('+') || a.StartsWith(':') || a.Contains(":+")));
                case "reset":
                    return Has("--hard", "--merge", "--keep");
                case "clean":
                case "rebase":
                    return true;
                case "restore":
                    // Only "--staged" alone is harmless (it just unstages); anything else throws changes away.
                    return !(Has("--staged") && !Has("--worktree"));
                case "checkout":
                    return Has("--force", "--") || Short('f') || Short('B') || args.Contains(".");
                case "switch":
                    return Has("--force", "--discard-changes") || Short('f') || Short('C');
                case "branch":
                    return Short('D') || Short('M') || Has("--force") || Short('f');
                case "tag":
                    return Short('d') || Short('f') || Has("--delete", "--force");
                case "stash":
                    return args.Count > 0 && args[0] is "drop" or "clear";
                case "commit":
                    return Has("--amend");
                case "rm":
                    return !Has("--cached");
                case "remote":
                    return args.Count > 0 && args[0] is "add" or "remove" or "rm" or "rename" or "set-url" or "set-head";
                default:
                    return false;
            }
        }

        public static bool Publishes(string sub, IReadOnlyList<string> args) =>
            sub == "push" || (sub == "remote" && args.Count > 0 && args[0] is "add" or "set-url");

        // git accepts any unambiguous start of a long option ("--upload-p"), so a start of a forbidden option is refused too.
        private static void CheckOption(string sub, string arg)
        {
            var name = arg.Contains('=') ? arg[..arg.IndexOf('=')] : arg;
            var bad = Forbidden.Contains(name)
                      || (name.StartsWith("--", StringComparison.Ordinal) && name.Length > 2
                          && Forbidden.Any(f => f.StartsWith("--", StringComparison.Ordinal) && f.StartsWith(name, StringComparison.Ordinal)))
                      // Options glued to their value: -xCOMMAND (rebase), -cNAME=VALUE / -uPROGRAM (clone), -Ofile (diff order file).
                      || arg.StartsWith("-O", StringComparison.Ordinal)
                      || (sub == "rebase" && arg.StartsWith("-x", StringComparison.Ordinal))
                      || (sub == "clone" && (arg.StartsWith("-c", StringComparison.Ordinal) || arg.StartsWith("-u", StringComparison.Ordinal)));
            if (bad)
            {
                throw new AgentException($"The option \"{Shorten(name)}\" is not allowed.");
            }
        }

        private static void CheckText(string arg)
        {
            if (arg == null || arg.Length > MaxArgLength || arg.Any(ch => ch == '\0' || ch == '\r' || ch == '\n'))
            {
                throw new AgentException("An argument for git is empty or not valid.");
            }
        }

        // A repository address: https, ssh or git only, on a host the person allowed. A name such as "origin"
        // or a branch is fine. Local paths are refused: they would let the site copy files from anywhere.
        public static void CheckRemoteText(string value, IReadOnlyCollection<string> allowedHosts)
        {
            if (value.Length == 0)
            {
                return;
            }

            if (value.Contains("::"))
            {
                throw new AgentException("This kind of repository address is not allowed.");
            }

            if (value.Contains("://"))
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !AllowedUrlSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
                {
                    throw new AgentException("Only https, ssh and git repository addresses are allowed.");
                }

                RequireHost(uri.Host, allowedHosts);
                return;
            }

            // scp-like: git@github.com:owner/name.git
            var at = value.IndexOf('@');
            var colon = value.IndexOf(':');
            if (at > 0 && colon > at && !value.Contains('\\') && !(colon == 1))
            {
                RequireHost(value.Substring(at + 1, colon - at - 1), allowedHosts);
                return;
            }

            var rooted = value.Length >= 2 && value[1] == ':' && char.IsLetter(value[0]);
            if (rooted || value.StartsWith('/') || value.StartsWith('\\') || value.StartsWith("./", StringComparison.Ordinal)
                || value.StartsWith("../", StringComparison.Ordinal) || value.StartsWith(".\\", StringComparison.Ordinal)
                || value.StartsWith("..\\", StringComparison.Ordinal) || value == "." || value == "..")
            {
                throw new AgentException("Local paths are not allowed as a repository address.");
            }
        }

        private static void RequireHost(string host, IReadOnlyCollection<string> allowedHosts)
        {
            // An empty list means "any host": the person did not restrict it. Pushing still asks first.
            if (allowedHosts.Count > 0 && !allowedHosts.Contains(host.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
            {
                throw new AgentException($"The host \"{Shorten(host)}\" is not in the list of Git hosts this PC allows (gitHosts).");
            }
        }

        private static string Shorten(string text) => text.Length > 60 ? text[..60] + "..." : text;
    }
}
