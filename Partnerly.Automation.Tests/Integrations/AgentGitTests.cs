using System.Diagnostics;
using System.Text.Json.Nodes;
using Partnerly.Agent.Commands;
using Partnerly.Agent.Config;
using Partnerly.Agent.Security;

namespace Partnerly.Automation.Tests.Integrations
{
    // GitGuard and GitRunner are compiled into this project straight from Partnerly.Agent.
    public class GitGuardTests
    {
        private static readonly string[] Hosts = { "github.com", "gitlab.com" };

        private static GitPlan Check(string sub, params string[] args) => GitGuard.Validate(sub, args, Hosts);

        [Theory]
        [InlineData("status")]
        [InlineData("log")]
        [InlineData("diff")]
        [InlineData("commit")]
        [InlineData("push")]
        [InlineData("rebase")]
        [InlineData("STATUS")]
        public void Known_sub_commands_pass(string sub)
        {
            Assert.NotNull(Check(sub));
        }

        [Theory]
        [InlineData("config")]
        [InlineData("submodule")]
        [InlineData("filter-branch")]
        [InlineData("daemon")]
        [InlineData("credential")]
        [InlineData("difftool")]
        [InlineData("gc")]
        [InlineData("apply")]
        [InlineData("alias-that-runs-a-shell")]
        [InlineData("")]
        public void Sub_commands_that_run_programs_or_change_settings_are_refused(string sub)
        {
            Assert.Throws<AgentException>(() => Check(sub));
        }

        [Theory]
        [InlineData("fetch", "--upload-pack=calc.exe")]
        [InlineData("fetch", "--upload-p=calc.exe")]
        [InlineData("push", "--receive-pack=calc.exe")]
        [InlineData("rebase", "--exec=calc.exe")]
        [InlineData("rebase", "-xcalc.exe")]
        [InlineData("diff", "--ext-diff")]
        [InlineData("diff", "--no-index")]
        [InlineData("diff", "--output=C:/x.txt")]
        [InlineData("diff", "--outp=C:/x.txt")]
        [InlineData("status", "--git-dir=C:/other")]
        [InlineData("status", "--work-tree=C:/other")]
        [InlineData("clone", "-c")]
        [InlineData("clone", "-ccore.sshCommand=calc")]
        [InlineData("clone", "-ucalc.exe")]
        [InlineData("clone", "--recurse-submodules")]
        [InlineData("commit", "-F")]
        [InlineData("commit", "--file=C:/secret.txt")]
        [InlineData("blame", "--contents=C:/secret.txt")]
        [InlineData("grep", "-Ocalc.exe")]
        public void Options_that_start_programs_or_touch_other_files_are_refused(string sub, string option)
        {
            Assert.Throws<AgentException>(() => Check(sub, option));
        }

        [Theory]
        [InlineData("fetch", "ext::sh -c calc")]
        [InlineData("clone", "file:///C:/Users/me/secret")]
        [InlineData("clone", "C:/Users/me/secret")]
        [InlineData("clone", "../secret")]
        [InlineData("clone", "http://example.com/a.git")]
        [InlineData("clone", "https://evil.example/a.git")]
        [InlineData("push", "https://evil.example/a.git")]
        [InlineData("remote", "git@evil.example:a/b.git")]
        public void Local_paths_odd_transports_and_unknown_hosts_are_refused_as_addresses(string sub, string address)
        {
            Assert.Throws<AgentException>(() => Check(sub, address));
        }

        [Theory]
        [InlineData("clone", "https://github.com/octocat/hello.git")]
        [InlineData("fetch", "origin")]
        [InlineData("push", "origin")]
        [InlineData("remote", "git@github.com:octocat/hello.git")]
        [InlineData("clone", "ssh://git@gitlab.com/a/b.git")]
        public void Allowed_hosts_and_remote_names_pass(string sub, string address)
        {
            Assert.NotNull(Check(sub, address));
        }

        [Theory]
        [InlineData("push", new[] { "--force" }, true)]
        [InlineData("push", new[] { "-f", "origin" }, true)]
        [InlineData("push", new[] { "--force-with-lease" }, true)]
        [InlineData("push", new[] { "origin", "+main" }, true)]
        [InlineData("push", new[] { "origin", ":old-branch" }, true)]
        [InlineData("push", new[] { "origin", "main" }, false)]
        [InlineData("reset", new[] { "--hard", "HEAD~1" }, true)]
        [InlineData("reset", new[] { "--soft", "HEAD~1" }, false)]
        [InlineData("clean", new[] { "-fd" }, true)]
        [InlineData("rebase", new[] { "main" }, true)]
        [InlineData("restore", new[] { "file.txt" }, true)]
        [InlineData("restore", new[] { "--staged", "file.txt" }, false)]
        [InlineData("branch", new[] { "-D", "old" }, true)]
        [InlineData("branch", new[] { "feature" }, false)]
        [InlineData("commit", new[] { "--amend", "-m", "x" }, true)]
        [InlineData("commit", new[] { "-m", "x" }, false)]
        [InlineData("stash", new[] { "drop" }, true)]
        [InlineData("stash", new[] { "push" }, false)]
        [InlineData("status", new string[0], false)]
        public void Risky_commands_are_recognised(string sub, string[] args, bool risky)
        {
            Assert.Equal(risky, GitGuard.Validate(sub, args, Hosts).Risky);
        }

        [Fact]
        public void Push_is_marked_as_sending_data_away()
        {
            Assert.True(Check("push", "origin").Publishes);
            Assert.True(Check("remote", "add", "x", "https://github.com/a/b.git").Publishes);
            Assert.False(Check("fetch", "origin").Publishes);
        }

        [Fact]
        public void Line_breaks_and_nul_inside_an_argument_are_refused()
        {
            Assert.Throws<AgentException>(() => Check("commit", "-m", "a\nb"));
            Assert.Throws<AgentException>(() => Check("commit", "-m", "a\0b"));
        }
    }

    // Real git in a temporary folder. Skipped (passes) on a machine without git.
    [Collection("AgentHome")]
    public class GitRunnerTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "gitrun-" + Guid.NewGuid().ToString("N"));
        private readonly string _home = Path.Combine(Path.GetTempPath(), "gitrun-home-" + Guid.NewGuid().ToString("N"));
        private readonly string? _oldHome = Environment.GetEnvironmentVariable("PARTNERLY_AGENT_HOME");
        private readonly AgentConfig _config;
        private readonly List<string> _asked = new();
        private bool _answer = true;
        private readonly GitRunner _runner;
        private readonly bool _hasGit;

        public GitRunnerTests()
        {
            Directory.CreateDirectory(_root);
            Environment.SetEnvironmentVariable("PARTNERLY_AGENT_HOME", _home);
            _config = new AgentConfig { AllowedFolders = new List<string> { _root }, GitHosts = new List<string> { "github.com" } };
            _runner = new GitRunner(_config, text => { _asked.Add(text); return _answer; });
            _hasGit = CanRunGit();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PARTNERLY_AGENT_HOME", _oldHome);
            foreach (var folder in new[] { _root, _home })
            {
                try { ForceDelete(folder); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        private static void ForceDelete(string folder)
        {
            if (!Directory.Exists(folder)) { return; }
            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(folder, true);
        }

        private static bool CanRunGit()
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo("git", "--version") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
                process!.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        private static JsonObject Command(string repo, string sub, params string[] args) => new()
        {
            ["repo"] = repo,
            ["sub"] = sub,
            ["args"] = new JsonArray(args.Select(item => (JsonNode?)JsonValue.Create(item)).ToArray())
        };

        private async Task<string> NewRepoAsync(string name = "repo")
        {
            var folder = Path.Combine(_root, name);
            await _runner.RunAsync(Command(folder, "init", "--initial-branch=main"), CancellationToken.None);
            // The commits of these tests need an author; set it for this repository only.
            foreach (var (key, value) in new[] { ("user.name", "Test"), ("user.email", "test@example.com"), ("commit.gpgsign", "false") })
            {
                await RawAsync(folder, "config", key, value);
            }

            return folder;
        }

        private static async Task RawAsync(string folder, params string[] args)
        {
            var info = new ProcessStartInfo("git") { WorkingDirectory = folder, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in args) { info.ArgumentList.Add(arg); }
            using var process = Process.Start(info)!;
            await process.WaitForExitAsync();
        }

        [Fact]
        public async Task Init_add_commit_status_and_log_work_end_to_end()
        {
            if (!_hasGit) { return; }
            var folder = await NewRepoAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "hello");

            var dirty = await _runner.RunAsync(Command(folder, "status", "--porcelain=v1", "-b"), CancellationToken.None);
            Assert.Contains("?? a.txt", dirty["output"]!.ToString());

            var commit = Command(folder, "commit", "-m", "First commit");
            commit["add"] = "all";
            var committed = await _runner.RunAsync(commit, CancellationToken.None);
            Assert.True(committed["committed"]!.GetValue<bool>());
            Assert.Equal("First commit", committed["subject"]!.ToString());
            Assert.Equal(40, committed["sha"]!.ToString().Length);

            var clean = await _runner.RunAsync(Command(folder, "status", "--porcelain=v1", "-b"), CancellationToken.None);
            Assert.DoesNotContain("a.txt", clean["output"]!.ToString());

            var nothing = Command(folder, "commit", "-m", "Again");
            nothing["add"] = "all";
            var second = await _runner.RunAsync(nothing, CancellationToken.None);
            Assert.False(second["committed"]!.GetValue<bool>());
        }

        [Fact]
        public async Task A_folder_outside_the_allowed_repositories_is_refused()
        {
            if (!_hasGit) { return; }
            var outside = Path.Combine(Path.GetTempPath(), "gitrun-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outside);
            try
            {
                var ex = await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(Command(outside, "status"), CancellationToken.None));
                Assert.Contains("allowedFolders", ex.Message);
            }
            finally
            {
                Directory.Delete(outside, true);
            }
        }

        [Fact]
        public async Task A_plain_folder_is_not_a_repository()
        {
            if (!_hasGit) { return; }
            var plain = Path.Combine(_root, "plain");
            Directory.CreateDirectory(plain);

            var ex = await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(Command(plain, "status"), CancellationToken.None));

            Assert.Contains("not a Git repository", ex.Message);
        }

        [Fact]
        public async Task Hooks_of_the_repository_do_not_run()
        {
            if (!_hasGit) { return; }
            var folder = await NewRepoAsync();
            var marker = Path.Combine(folder, "hook-ran.txt");
            var hook = Path.Combine(folder, ".git", "hooks", "pre-commit");
            Directory.CreateDirectory(Path.GetDirectoryName(hook)!);
            await File.WriteAllTextAsync(hook, "#!/bin/sh\necho ran > \"" + marker.Replace('\\', '/') + "\"\n");
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "x");
            var commit = Command(folder, "commit", "-m", "x");
            commit["add"] = "all";

            await _runner.RunAsync(commit, CancellationToken.None);

            Assert.False(File.Exists(marker));
        }

        [Fact]
        public async Task Risky_command_asks_and_a_no_stops_it()
        {
            if (!_hasGit) { return; }
            var folder = await NewRepoAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "x");
            var commit = Command(folder, "commit", "-m", "x");
            commit["add"] = "all";
            await _runner.RunAsync(commit, CancellationToken.None);
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "changed");
            _asked.Clear();
            _answer = false;

            var ex = await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(Command(folder, "reset", "--hard"), CancellationToken.None));

            Assert.Contains("declined", ex.Message);
            Assert.Single(_asked);
            Assert.Contains("git reset --hard", _asked[0]);
            Assert.Equal("changed", await File.ReadAllTextAsync(Path.Combine(folder, "a.txt")));
        }

        [Fact]
        public async Task Safe_command_does_not_ask()
        {
            if (!_hasGit) { return; }
            var folder = await NewRepoAsync();
            _asked.Clear();

            await _runner.RunAsync(Command(folder, "status"), CancellationToken.None);

            Assert.Empty(_asked);
        }

        [Fact]
        public async Task Push_always_asks_unless_the_person_switched_it_off()
        {
            if (!_hasGit) { return; }
            var folder = await NewRepoAsync();
            _answer = false;

            await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(Command(folder, "push", "origin", "main"), CancellationToken.None));

            Assert.Single(_asked);
        }

        [Fact]
        public async Task Clone_needs_a_real_address_and_an_allowed_host()
        {
            if (!_hasGit) { return; }
            var target = Path.Combine(_root, "copy");

            await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(Command(target, "clone", "somefolder"), CancellationToken.None));
            await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(Command(target, "clone", "https://evil.example/a.git"), CancellationToken.None));
            await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(Command(Path.Combine(Path.GetTempPath(), "elsewhere"), "clone", "https://github.com/a/b.git"), CancellationToken.None));
        }

        [Fact]
        public async Task Branch_switch_and_log_after_a_second_commit()
        {
            if (!_hasGit) { return; }
            var folder = await NewRepoAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "1");
            var first = Command(folder, "commit", "-m", "one");
            first["add"] = "all";
            await _runner.RunAsync(first, CancellationToken.None);
            await _runner.RunAsync(Command(folder, "switch", "-c", "feature"), CancellationToken.None);
            await File.WriteAllTextAsync(Path.Combine(folder, "b.txt"), "2");
            var second = Command(folder, "commit", "-m", "two");
            second["add"] = "paths";
            second["paths"] = new JsonArray("b.txt");
            await _runner.RunAsync(second, CancellationToken.None);

            var log = await _runner.RunAsync(Command(folder, "log", "-n", "5", "--pretty=format:%s"), CancellationToken.None);
            var branches = await _runner.RunAsync(Command(folder, "branch", "--list", "--format=%(refname:short)"), CancellationToken.None);

            Assert.Equal("two\none", log["output"]!.ToString().Replace("\r", string.Empty));
            Assert.Contains("feature", branches["output"]!.ToString());
        }

        [Fact]
        public async Task Add_paths_cannot_leave_the_repository()
        {
            if (!_hasGit) { return; }
            var folder = await NewRepoAsync();
            var commit = Command(folder, "commit", "-m", "x");
            commit["add"] = "paths";
            commit["paths"] = new JsonArray("../outside.txt");

            await Assert.ThrowsAsync<AgentException>(() => _runner.RunAsync(commit, CancellationToken.None));
        }
    }
}
