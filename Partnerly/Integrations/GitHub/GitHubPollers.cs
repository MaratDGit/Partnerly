using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.GitHub
{
    // GitHub webhooks need a public address, so triggers look at the repository once a minute instead
    // (works on localhost too). The first pass only remembers where things stand; it never replays history.
    // Cursors live in the connection payload ("githubCursors"), one per trigger + repository (+ branch).
    public abstract class GitHubPollerBase : IIntegrationPoller
    {
        private const int MaxPerPass = 20;

        private readonly AutomationTriggerDispatcher _dispatcher;

        protected GitHubPollerBase(GitHubApiClient api, AutomationTriggerDispatcher dispatcher)
        {
            Api = api;
            _dispatcher = dispatcher;
        }

        protected GitHubApiClient Api { get; }

        public abstract string TriggerType { get; }
        public string ConnectionKind => IntegrationKinds.GitHub;
        public TimeSpan Interval => TimeSpan.FromSeconds(60);

        // New events of one repository after the cursor, oldest first, and the cursor to remember.
        // A null cursor on entry means "first pass".
        protected abstract Task<(List<JsonObject> Events, string Cursor)> FetchAsync(
            IntegrationConnection connection, string repo, string branch, string? cursor, CancellationToken cancellationToken);

        // Extra filters of a trigger card (title contains...); the repository and branch are checked by the poller.
        protected virtual bool Matches(JsonElement config, JsonObject payload) => true;

        protected virtual bool UsesBranch => false;

        public async Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken)
        {
            var connection = target.Connection;
            var cursors = connection.Payload["githubCursors"] as JsonObject ?? new JsonObject();
            connection.Payload["githubCursors"] = cursors;

            var watched = new List<(string Repo, string Branch)>();
            foreach (var config in target.TriggerConfigs)
            {
                try
                {
                    var item = (GitHubApiClient.Repo(Read(config, "repo")), UsesBranch ? Read(config, "branch") : string.Empty);
                    if (!watched.Contains(item)) { watched.Add(item); }
                }
                catch (GitHubSettingException)
                {
                    // a trigger card without a repository yet: nothing to watch
                }
            }

            foreach (var (repo, branch) in watched)
            {
                var key = $"{TriggerType}|{repo}|{branch}";
                var cursor = cursors[key]?.ToString();
                var (events, next) = await FetchAsync(connection, repo, branch, string.IsNullOrEmpty(cursor) ? null : cursor, cancellationToken);
                cursors[key] = next;
                if (string.IsNullOrEmpty(cursor))
                {
                    continue;
                }

                foreach (var payload in events.Take(MaxPerPass))
                {
                    var current = payload;
                    await _dispatcher.EnqueueIntegrationAsync(
                        TriggerType,
                        connection.OwnerUserId,
                        connection.Id,
                        current,
                        config => SameTarget(config, repo, branch) && Matches(config, current),
                        cancellationToken);
                }
            }
        }

        private bool SameTarget(JsonElement config, string repo, string branch)
        {
            try
            {
                return string.Equals(GitHubApiClient.Repo(Read(config, "repo")), repo, StringComparison.OrdinalIgnoreCase)
                       && (!UsesBranch || string.Equals(Read(config, "branch"), branch, StringComparison.Ordinal));
            }
            catch (GitHubSettingException)
            {
                return false;
            }
        }

        protected static string Read(JsonElement config, string name) =>
            config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value)
                ? value.ToString().Trim()
                : string.Empty;

        protected static bool TitleMatches(JsonElement config, JsonObject payload)
        {
            var contains = Read(config, "titleContains");
            return contains.Length == 0
                   || (payload["title"]?.ToString() ?? string.Empty).Contains(contains, StringComparison.OrdinalIgnoreCase);
        }

        protected static long Number(JsonNode? node, string key) =>
            long.TryParse((node as JsonObject)?[key]?.ToString(), out var value) ? value : 0;
    }

    public sealed class GitHubIssueOpenedPoller : GitHubPollerBase
    {
        public GitHubIssueOpenedPoller(GitHubApiClient api, AutomationTriggerDispatcher dispatcher) : base(api, dispatcher) { }
        public override string TriggerType => IntegrationTypeCodes.GitHubIssueOpened;
        protected override bool Matches(JsonElement config, JsonObject payload) => TitleMatches(config, payload);

        protected override async Task<(List<JsonObject>, string)> FetchAsync(
            IntegrationConnection connection, string repo, string branch, string? cursor, CancellationToken cancellationToken)
        {
            var items = await Api.ListAsync(connection, $"/repos/{repo}/issues?state=all&sort=created&direction=desc", 30, cancellationToken);
            var known = long.TryParse(cursor, out var parsed) ? parsed : -1;
            var fresh = items.Where(item => (item as JsonObject)?["pull_request"] == null && Number(item, "number") > known)
                .OrderBy(item => Number(item, "number")).ToList();
            var newest = Math.Max(known, items.Select(item => Number(item, "number")).DefaultIfEmpty(0).Max());
            return (fresh.Select(item => GitHubShapes.Issue(item, repo)).ToList(), newest.ToString());
        }
    }

    public sealed class GitHubPullRequestOpenedPoller : GitHubPollerBase
    {
        public GitHubPullRequestOpenedPoller(GitHubApiClient api, AutomationTriggerDispatcher dispatcher) : base(api, dispatcher) { }
        public override string TriggerType => IntegrationTypeCodes.GitHubPullRequestOpened;
        protected override bool Matches(JsonElement config, JsonObject payload) => TitleMatches(config, payload);

        protected override async Task<(List<JsonObject>, string)> FetchAsync(
            IntegrationConnection connection, string repo, string branch, string? cursor, CancellationToken cancellationToken)
        {
            var items = await Api.ListAsync(connection, $"/repos/{repo}/pulls?state=all&sort=created&direction=desc", 30, cancellationToken);
            var known = long.TryParse(cursor, out var parsed) ? parsed : -1;
            var fresh = items.Where(item => Number(item, "number") > known).OrderBy(item => Number(item, "number")).ToList();
            var newest = Math.Max(known, items.Select(item => Number(item, "number")).DefaultIfEmpty(0).Max());
            return (fresh.Select(item => GitHubShapes.PullRequest(item, repo)).ToList(), newest.ToString());
        }
    }

    // New commits on a branch (an empty branch means the default one).
    public sealed class GitHubPushPoller : GitHubPollerBase
    {
        public GitHubPushPoller(GitHubApiClient api, AutomationTriggerDispatcher dispatcher) : base(api, dispatcher) { }
        public override string TriggerType => IntegrationTypeCodes.GitHubPush;
        protected override bool UsesBranch => true;

        protected override async Task<(List<JsonObject>, string)> FetchAsync(
            IntegrationConnection connection, string repo, string branch, string? cursor, CancellationToken cancellationToken)
        {
            var query = branch.Length > 0 ? "?sha=" + Uri.EscapeDataString(branch) : string.Empty;
            var items = await Api.ListAsync(connection, $"/repos/{repo}/commits{query}", 30, cancellationToken);
            var latest = items.Count > 0 ? GitHubShapes.Text(items[0], "sha") : cursor ?? string.Empty;
            var fresh = new List<JsonObject>();
            foreach (var item in items)
            {
                if (cursor != null && GitHubShapes.Text(item, "sha") == cursor)
                {
                    break;
                }

                fresh.Add(GitHubShapes.Commit(item, repo, branch));
            }

            // The list is newest first; runs should see the oldest commit first.
            fresh.Reverse();
            return (fresh, latest);
        }
    }

    public sealed class GitHubReleasePublishedPoller : GitHubPollerBase
    {
        public GitHubReleasePublishedPoller(GitHubApiClient api, AutomationTriggerDispatcher dispatcher) : base(api, dispatcher) { }
        public override string TriggerType => IntegrationTypeCodes.GitHubReleasePublished;

        protected override async Task<(List<JsonObject>, string)> FetchAsync(
            IntegrationConnection connection, string repo, string branch, string? cursor, CancellationToken cancellationToken)
        {
            var items = await Api.ListAsync(connection, $"/repos/{repo}/releases", 10, cancellationToken);
            var known = long.TryParse(cursor, out var parsed) ? parsed : -1;
            var fresh = items.Where(item => Number(item, "id") > known && (item as JsonObject)?["draft"]?.GetValue<bool>() != true)
                .OrderBy(item => Number(item, "id")).ToList();
            var newest = Math.Max(known, items.Select(item => Number(item, "id")).DefaultIfEmpty(0).Max());
            return (fresh.Select(item => GitHubShapes.Release(item, repo)).ToList(), newest.ToString());
        }
    }

    public sealed class GitHubWorkflowFailedPoller : GitHubPollerBase
    {
        public GitHubWorkflowFailedPoller(GitHubApiClient api, AutomationTriggerDispatcher dispatcher) : base(api, dispatcher) { }
        public override string TriggerType => IntegrationTypeCodes.GitHubWorkflowFailed;
        protected override bool UsesBranch => true;

        protected override async Task<(List<JsonObject>, string)> FetchAsync(
            IntegrationConnection connection, string repo, string branch, string? cursor, CancellationToken cancellationToken)
        {
            var query = "status=failure" + (branch.Length > 0 ? "&branch=" + Uri.EscapeDataString(branch) : string.Empty);
            var items = await Api.ListAsync(connection, $"/repos/{repo}/actions/runs?{query}", 20, cancellationToken);
            var known = long.TryParse(cursor, out var parsed) ? parsed : -1;
            var fresh = items.Where(item => Number(item, "id") > known).OrderBy(item => Number(item, "id")).ToList();
            var newest = Math.Max(known, items.Select(item => Number(item, "id")).DefaultIfEmpty(0).Max());
            return (fresh.Select(item => GitHubShapes.WorkflowRun(item, repo)).ToList(), newest.ToString());
        }
    }
}
