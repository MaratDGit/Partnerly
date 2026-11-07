using System.Text.Json.Nodes;
using Partnerly.Agent.Config;
using Partnerly.Agent.Security;

namespace Partnerly.Automation.Tests.Integrations
{
    // The settings of Partnerly Agent are one list of folders; these tests cover merging an old file,
    // finding repositories by themselves, and the "Browse" listing.
    public class AgentSettingsTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "agset-" + Guid.NewGuid().ToString("N"));

        public AgentSettingsTests()
        {
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private string Folder(params string[] parts)
        {
            var path = Path.Combine(new[] { _root }.Concat(parts).ToArray());
            Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public void An_old_file_is_merged_into_one_list_of_folders_and_programs()
        {
            var config = new AgentConfig
            {
                AllowedFolders = new List<string> { "C:/Docs" },
                ScriptFolders = new List<string> { "C:/Scripts" },
                AllowedRepos = new List<string> { "C:/Dev", "c:/docs" },
                WatchFolders = new List<string> { "D:/Inbox" },
                AllowedPrograms = new List<string> { "C:/Tools/Notepad.exe" },
                WatchPrograms = new List<string> { "notepad", "chrome" },
                WatchRepos = new List<string> { "C:/Dev/app" }
            };

            config.Normalize();

            Assert.Equal(new[] { "C:/Docs", "C:/Dev", "C:/Scripts", "D:/Inbox" }.OrderBy(x => x), config.AllowedFolders.OrderBy(x => x));
            Assert.Equal(2, config.AllowedPrograms.Count);
            Assert.Contains("chrome", config.AllowedPrograms);
            Assert.Empty(config.ScriptFolders);
            Assert.Empty(config.AllowedRepos);
            Assert.Empty(config.WatchFolders);
            Assert.Empty(config.WatchPrograms);
            Assert.Empty(config.WatchRepos);
            Assert.Equal(2, config.SettingsVersion);
        }

        [Fact]
        public void Scripts_ask_first_unless_scripts_were_set_up_on_purpose()
        {
            var fresh = new AgentConfig { ConfirmOps = new List<string> { "shutdown" } };
            var deliberate = new AgentConfig { ScriptFolders = new List<string> { "C:/Scripts" }, ConfirmOps = new List<string> { "shutdown" } };

            fresh.Normalize();
            deliberate.Normalize();

            Assert.Contains("run_script", fresh.ConfirmOps);
            Assert.DoesNotContain("run_script", deliberate.ConfirmOps);
        }

        [Fact]
        public void Merging_twice_changes_nothing()
        {
            var config = new AgentConfig { AllowedFolders = new List<string> { "C:/Docs" }, ScriptFolders = new List<string> { "C:/Scripts" } };

            config.Normalize();
            var first = config.AllowedFolders.ToList();
            config.Normalize();

            Assert.Equal(first, config.AllowedFolders);
        }

        [Theory]
        [InlineData("C:\\Tools\\App.exe", "App")]
        [InlineData("app.EXE", "app")]
        [InlineData("chrome", "chrome")]
        [InlineData("\"C:/Program Files/X/y.exe\"", "y")]
        public void Program_names_are_compared_without_folder_and_extension(string value, string expected)
        {
            Assert.Equal(expected, AgentConfig.ProgramName(value));
        }

        [Fact]
        public void Repositories_are_found_inside_the_allowed_folders_but_not_in_build_output()
        {
            var work = Folder("work");
            Directory.CreateDirectory(Path.Combine(Folder("work", "app"), ".git"));
            Directory.CreateDirectory(Path.Combine(Folder("work", "group", "deep-repo"), ".git"));
            Directory.CreateDirectory(Path.Combine(Folder("work", "app", "vendor", "inner"), ".git"));
            Directory.CreateDirectory(Path.Combine(Folder("work", "node_modules", "pkg"), ".git"));
            Folder("work", "plain");

            var found = RepoFinder.Find(new[] { work });

            Assert.Equal(2, found.Count);
            Assert.Contains(found, item => item.EndsWith("app"));
            Assert.Contains(found, item => item.EndsWith("deep-repo"));
        }

        [Fact]
        public void An_allowed_folder_that_is_itself_a_repository_is_found()
        {
            var repo = Folder("single");
            Directory.CreateDirectory(Path.Combine(repo, ".git"));

            Assert.Equal(new[] { repo }, RepoFinder.Find(new[] { repo }));
        }

        [Fact]
        public void The_site_is_told_the_found_repositories_and_the_folders_as_script_places()
        {
            var work = Folder("work2");
            Directory.CreateDirectory(Path.Combine(Folder("work2", "app"), ".git"));
            var config = new AgentConfig { AllowedFolders = new List<string> { work } };

            var json = JsonNode.Parse(config.ToSiteJson())!;

            Assert.Equal(work, json["folders"]![0]!.ToString());
            Assert.Equal(work, json["scripts"]![0]!.ToString());
            Assert.EndsWith("app", json["repos"]![0]!.ToString());
        }

        [Fact]
        public void Browse_lists_the_allowed_folders_first_then_what_is_inside()
        {
            var allowed = Folder("allowed");
            Folder("allowed", "sub");
            File.WriteAllText(Path.Combine(allowed, "a.txt"), "hello");
            Directory.CreateDirectory(Path.Combine(Folder("allowed", "repo"), ".git"));
            var guard = new PathGuard(new[] { allowed });

            var roots = FolderBrowser.List(guard, null);
            var inside = FolderBrowser.List(guard, allowed);

            Assert.True(roots["isRoot"]!.GetValue<bool>());
            Assert.Equal(allowed, roots["items"]![0]!["path"]!.ToString());
            var names = inside["items"]!.AsArray().Select(item => item!["name"]!.ToString()).ToList();
            Assert.Equal(new[] { "repo", "sub", "a.txt" }, names);
            Assert.True(inside["items"]![0]!["isRepo"]!.GetValue<bool>());
            Assert.Equal(5, inside["items"]![2]!["size"]!.GetValue<long>());
            Assert.Equal(string.Empty, inside["parent"]!.ToString());
        }

        [Fact]
        public void Browse_offers_the_way_up_inside_the_allowed_folders_only()
        {
            var allowed = Folder("allowed2");
            var sub = Folder("allowed2", "sub");
            var guard = new PathGuard(new[] { allowed });

            var listing = FolderBrowser.List(guard, sub);

            Assert.Equal(allowed, listing["parent"]!.ToString());
        }

        [Fact]
        public void Browse_refuses_places_outside_the_allowed_folders()
        {
            var allowed = Folder("allowed3");
            var outside = Folder("outside3");
            var guard = new PathGuard(new[] { allowed });

            Assert.Throws<AgentException>(() => FolderBrowser.List(guard, outside));
            Assert.Throws<AgentException>(() => FolderBrowser.List(guard, Path.Combine(allowed, "..", "outside3")));
        }

        [Fact]
        public void Git_hosts_left_empty_mean_any_host_but_only_safe_kinds_of_address()
        {
            var none = Array.Empty<string>();

            Assert.NotNull(GitGuard.Validate("clone", new[] { "https://example.org/a.git" }, none));
            Assert.NotNull(GitGuard.Validate("remote", new[] { "add", "x", "git@example.org:a/b.git" }, none));
            Assert.Throws<AgentException>(() => GitGuard.Validate("clone", new[] { "ext::sh -c calc" }, none));
            Assert.Throws<AgentException>(() => GitGuard.Validate("clone", new[] { "C:/secret" }, none));
            Assert.Throws<AgentException>(() => GitGuard.Validate("clone", new[] { "https://example.org/a.git" }, new[] { "github.com" }));
        }
    }
}
