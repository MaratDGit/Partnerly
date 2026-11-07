using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.GitHub;

namespace Partnerly.Automation.Tests.Integrations
{
    public class GitHubIntegrationTests
    {
        private static JsonObject Payload() => new()
        {
            ["login"] = "octocat",
            ["accessToken"] = "gh-token",
            ["scope"] = "read:user repo"
        };

        [Theory]
        [InlineData("octocat/hello-world", "octocat/hello-world")]
        [InlineData("https://github.com/octocat/hello-world", "octocat/hello-world")]
        [InlineData("https://github.com/octocat/hello-world.git", "octocat/hello-world")]
        [InlineData("git@github.com:octocat/hello-world.git", "octocat/hello-world")]
        [InlineData("  octocat/hello-world/  ", "octocat/hello-world")]
        public void Repository_is_normalised(string input, string expected)
        {
            Assert.Equal(expected, GitHubApiClient.Repo(input));
        }

        [Theory]
        [InlineData("")]
        [InlineData("octocat")]
        [InlineData("../etc/passwd")]
        [InlineData("a/b/c")]
        [InlineData("octocat/hello world")]
        [InlineData("octocat/..")]
        public void Wrong_repository_is_refused(string input)
        {
            Assert.Throws<GitHubSettingException>(() => GitHubApiClient.Repo(input));
        }

        [Theory]
        [InlineData("/user/repos")]
        [InlineData("/repos/octocat/hello-world/issues?state=open")]
        public void Api_path_inside_the_api_is_accepted(string path)
        {
            Assert.Equal(path, GitHubApiClient.SafePath(path));
        }

        [Theory]
        [InlineData("https://evil.example/x")]
        [InlineData("//evil.example/x")]
        [InlineData("user/repos")]
        [InlineData("/repos/../admin")]
        [InlineData("/user repos")]
        [InlineData("")]
        public void Api_path_that_could_leave_the_api_is_refused(string path)
        {
            Assert.Throws<GitHubSettingException>(() => GitHubApiClient.SafePath(path));
        }

        [Fact]
        public async Task Create_issue_posts_title_labels_and_returns_number()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler()
                .On("/repos/octocat/hello-world/issues", "{\"number\":7,\"title\":\"Bug\",\"state\":\"open\",\"html_url\":\"https://github.com/octocat/hello-world/issues/7\",\"user\":{\"login\":\"octocat\"},\"labels\":[{\"name\":\"bug\"}]}", HttpStatusCode.Created, HttpMethod.Post);
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubCreateIssue, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "https://github.com/octocat/hello-world",
                ["title"] = "Bug",
                ["body"] = "It breaks",
                ["labels"] = "bug, urgent"
            });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var request = http.Requests.Single();
            Assert.Equal("Bearer gh-token", request.Authorization);
            var body = JsonNode.Parse(request.Body)!;
            Assert.Equal("Bug", body["title"]!.ToString());
            Assert.Equal(new[] { "bug", "urgent" }, body["labels"]!.AsArray().Select(item => item!.ToString()).ToArray());
            Assert.Contains("\"number\":7", step!.OutputJson);
            Assert.DoesNotContain("gh-token", step.OutputJson ?? string.Empty);
        }

        [Fact]
        public async Task Writes_are_skipped_in_test_run_and_reads_still_run()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/repos/octocat/hello-world", "{\"full_name\":\"octocat/hello-world\",\"default_branch\":\"main\"}");
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var write = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubCreateIssue, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world",
                ["title"] = "x"
            });
            var read = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubGetRepo, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world"
            });

            var (_, writeStep) = await IntegrationTestHost.RunAsync(provider, write, dryRun: true);
            var (_, readStep) = await IntegrationTestHost.RunAsync(provider, read, dryRun: true);

            Assert.Equal(AutomationStatuses.StepSkipped, writeStep!.Status);
            Assert.Equal(AutomationStatuses.StepSucceeded, readStep!.Status);
            Assert.Contains("octocat/hello-world", readStep.OutputJson);
        }

        [Fact]
        public async Task A_missing_setting_fails_the_card_but_not_the_connection()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubGetIssue, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world",
                ["number"] = "abc"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("number", step.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(http.Requests);
            Assert.True((await IntegrationTestHost.ReloadAsync(provider, connection.Id)).Status.Ok);
        }

        [Fact]
        public async Task Another_users_connection_is_refused()
        {
            var owner = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http);
            var foreign = await IntegrationTestHost.ConnectAsync(provider, stranger, IntegrationKinds.GitHub, Payload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubGetRepo, new JsonObject
            {
                ["connectionId"] = foreign.Id.ToString(),
                ["repo"] = "octocat/hello-world"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Revoked_token_asks_to_reconnect_and_marks_the_connection()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/repos/", "{\"message\":\"Bad credentials\"}", HttpStatusCode.Unauthorized);
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubGetRepo, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("Reconnect GitHub", step.Error);
            Assert.False((await IntegrationTestHost.ReloadAsync(provider, connection.Id)).Status.Ok);
        }

        [Fact]
        public async Task Merge_pull_request_uses_the_chosen_method()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/pulls/5/merge", "{\"merged\":true,\"sha\":\"abc\",\"message\":\"Pull Request successfully merged\"}");
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubMergePullRequest, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world",
                ["number"] = "#5",
                ["method"] = "squash"
            });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var request = http.Requests.Single();
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("squash", JsonNode.Parse(request.Body)!["merge_method"]!.ToString());
            Assert.Contains("\"merged\":true", step!.OutputJson);
        }

        [Fact]
        public async Task Put_file_updates_an_existing_file_with_its_sha()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler()
                .On("/contents/docs/readme.md", req => req.Method == HttpMethod.Get
                    ? (HttpStatusCode.OK, "{\"sha\":\"old-sha\"}")
                    : (HttpStatusCode.OK, "{\"commit\":{\"sha\":\"new-commit\"},\"content\":{\"html_url\":\"https://github.com/o/r/blob/main/docs/readme.md\"}}"));
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubPutFile, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world",
                ["path"] = "docs/readme.md",
                ["text"] = "hello",
                ["message"] = "Update readme"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var put = http.Requests.Single(item => item.Method == HttpMethod.Put);
            var body = JsonNode.Parse(put.Body)!;
            Assert.Equal("old-sha", body["sha"]!.ToString());
            Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hello")), body["content"]!.ToString());
            Assert.Contains("new-commit", step.OutputJson);
        }

        [Fact]
        public async Task Api_request_cannot_leave_api_github_com()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GitHubApiRequest, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["method"] = "GET",
                ["path"] = "https://evil.example/steal"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Issue_poller_starts_from_now_then_reports_new_issues_but_not_pull_requests()
        {
            var owner = Guid.NewGuid();
            var calls = 0;
            var http = new FakeHttpHandler().On("/repos/octocat/hello-world/issues", _ =>
            {
                calls++;
                return calls == 1
                    ? (HttpStatusCode.OK, "[{\"number\":3,\"title\":\"old\",\"user\":{\"login\":\"a\"}}]")
                    : (HttpStatusCode.OK, "[{\"number\":5,\"title\":\"PR\",\"pull_request\":{},\"user\":{\"login\":\"a\"}},{\"number\":4,\"title\":\"Crash on start\",\"user\":{\"login\":\"b\"}},{\"number\":3,\"title\":\"old\",\"user\":{\"login\":\"a\"}}]");
            });
            await using var provider = IntegrationTestHost.Create(owner, http);
            await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var connection = (await provider.CreateScope().ServiceProvider.GetRequiredService<IIntegrationConnectionService>().ListAsync(owner)).Single();
            var automation = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.GitHubIssueOpened, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world",
                ["titleContains"] = "crash"
            });

            await IntegrationTestHost.PollAsync(provider);
            Assert.Empty(IntegrationTestHost.RunsOf(provider, automation));
            await IntegrationTestHost.PollAsync(provider);

            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, automation));
            var payload = IntegrationTestHost.TriggerPayload(run);
            Assert.Equal("4", payload["number"]!.ToString());
            Assert.Equal("Crash on start", payload["title"]!.ToString());
        }

        [Fact]
        public async Task Push_poller_reports_new_commits_oldest_first()
        {
            var owner = Guid.NewGuid();
            var calls = 0;
            var http = new FakeHttpHandler().On("/repos/octocat/hello-world/commits", _ =>
            {
                calls++;
                return calls == 1
                    ? (HttpStatusCode.OK, "[{\"sha\":\"c1\",\"commit\":{\"message\":\"first\",\"author\":{\"name\":\"A\",\"date\":\"2026-11-01T10:00:00Z\"}}}]")
                    : (HttpStatusCode.OK, "[{\"sha\":\"c3\",\"commit\":{\"message\":\"third\\n\\nmore\",\"author\":{\"name\":\"A\"}}},{\"sha\":\"c2\",\"commit\":{\"message\":\"second\",\"author\":{\"name\":\"A\"}}},{\"sha\":\"c1\",\"commit\":{\"message\":\"first\",\"author\":{\"name\":\"A\"}}}]");
            });
            await using var provider = IntegrationTestHost.Create(owner, http);
            await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.GitHub, Payload());
            var connection = (await provider.CreateScope().ServiceProvider.GetRequiredService<IIntegrationConnectionService>().ListAsync(owner)).Single();
            var automation = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.GitHubPush, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["repo"] = "octocat/hello-world"
            });

            await IntegrationTestHost.PollAsync(provider);
            await IntegrationTestHost.PollAsync(provider);

            // The agent runs one at a time (MaxConcurrentRuns = 1), so only the first new commit starts a run
            // while it is still queued; it must be the OLDEST new commit.
            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, automation));
            var payload = IntegrationTestHost.TriggerPayload(run);
            Assert.Equal("second", payload["subject"]!.ToString());
            Assert.Equal("c2", payload["sha"]!.ToString());
        }
    }
}
