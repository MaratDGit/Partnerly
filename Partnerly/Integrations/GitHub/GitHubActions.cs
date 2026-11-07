using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.GitHub
{
    // What one GitHub card needs while it runs: its settings with {{variables}} already filled in.
    public sealed class GitHubCall
    {
        private readonly ActionContext _context;
        private readonly IVariableBinder _binder;

        public GitHubCall(ActionContext context, IntegrationConnection connection, IVariableBinder binder, CancellationToken cancellationToken)
        {
            _context = context;
            Connection = connection;
            _binder = binder;
            CancellationToken = cancellationToken;
        }

        public IntegrationConnection Connection { get; }
        public CancellationToken CancellationToken { get; }

        public string Text(string key) =>
            _binder.Interpolate(NodeConfig.GetString(_context.Node, key), _context.Variables).Trim();

        public string Repo() => GitHubApiClient.Repo(Text("repo"));

        public int Int(string key, int fallback, int min, int max) =>
            int.TryParse(Text(key), out var value) ? Math.Clamp(value, min, max) : fallback;

        public bool Flag(string key) => string.Equals(Text(key), "true", StringComparison.OrdinalIgnoreCase);

        public List<string> List(string key) =>
            Text(key).Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        public string Required(string key, string label)
        {
            var value = Text(key);
            return value.Length > 0 ? value : throw new GitHubSettingException(label + " is required.", retryable: false);
        }

        public int Number(string key = "number")
        {
            var text = Text(key).TrimStart('#');
            return int.TryParse(text, out var number) && number > 0
                ? number
                : throw new GitHubSettingException("Issue or pull request number is required.", retryable: false);
        }
    }

    public abstract class GitHubActionBase : IntegrationActionHandlerBase
    {
        protected GitHubActionBase(GitHubApiClient api)
        {
            Api = api;
        }

        protected GitHubApiClient Api { get; }

        public override string Group => "GitHub";
        public override string ConnectionKind => IntegrationKinds.GitHub;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            try
            {
                var output = await RunAsync(new GitHubCall(context, connection, binder, cancellationToken));
                return StepResult.Succeeded(output);
            }
            catch (GitHubSettingException ex)
            {
                // A wrong card setting (no repository, no number) is the card's problem, not the connection's.
                return StepResult.Failed(ex.Message, retryable: false);
            }
        }

        protected abstract Task<JsonObject> RunAsync(GitHubCall call);

        protected Task<JsonObject> Object(GitHubCall call, HttpMethod method, string path, JsonNode? body = null) =>
            Api.SendObjectAsync(call.Connection, method, path, body, call.CancellationToken);

        protected Task<JsonArray> Array(GitHubCall call, string path, int max) =>
            Api.ListAsync(call.Connection, path, max, call.CancellationToken);

        // Adds a setting to the request only when the person filled it in.
        protected static void Put(JsonObject body, string key, string value)
        {
            if (value.Length > 0)
            {
                body[key] = value;
            }
        }

        protected static JsonArray Strings(IEnumerable<string> values) => new(values.Select(item => (JsonNode?)JsonValue.Create(item)).ToArray());

        protected static string Qs(params (string Key, string Value)[] pairs) =>
            string.Join("&", pairs.Where(pair => pair.Value.Length > 0).Select(pair => pair.Key + "=" + Uri.EscapeDataString(pair.Value)));
    }

    // ---------- repositories ----------
    public sealed class GitHubListReposAction : GitHubActionBase
    {
        public GitHubListReposAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubListRepos;
        public override string DisplayName => "List my GitHub repositories";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var max = call.Int("max", 30, 1, 100);
            var items = await Array(call, "/user/repos?sort=updated&affiliation=owner,collaborator,organization_member", max);
            var contains = call.Text("nameContains");
            return GitHubShapes.List(GitHubShapes.Many(items, GitHubShapes.Repo,
                item => contains.Length == 0 || GitHubShapes.Text(item, "full_name").Contains(contains, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public sealed class GitHubGetRepoAction : GitHubActionBase
    {
        public GitHubGetRepoAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubGetRepo;
        public override string DisplayName => "Get GitHub repository";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call) =>
            GitHubShapes.Repo(await Object(call, HttpMethod.Get, "/repos/" + call.Repo()));
    }

    // ---------- issues ----------
    public sealed class GitHubCreateIssueAction : GitHubActionBase
    {
        public GitHubCreateIssueAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubCreateIssue;
        public override string DisplayName => "Create GitHub issue";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var body = new JsonObject { ["title"] = call.Required("title", "Title") };
            Put(body, "body", call.Text("body"));
            var labels = call.List("labels");
            if (labels.Count > 0) { body["labels"] = Strings(labels); }
            var assignees = call.List("assignees");
            if (assignees.Count > 0) { body["assignees"] = Strings(assignees); }
            return GitHubShapes.Issue(await Object(call, HttpMethod.Post, $"/repos/{repo}/issues", body), repo);
        }
    }

    public sealed class GitHubGetIssueAction : GitHubActionBase
    {
        public GitHubGetIssueAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubGetIssue;
        public override string DisplayName => "Get GitHub issue";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            return GitHubShapes.Issue(await Object(call, HttpMethod.Get, $"/repos/{repo}/issues/{call.Number()}"), repo);
        }
    }

    public sealed class GitHubListIssuesAction : GitHubActionBase
    {
        public GitHubListIssuesAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubListIssues;
        public override string DisplayName => "Find GitHub issues";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var state = call.Text("state") is "closed" or "all" ? call.Text("state") : "open";
            var query = Qs(("state", state), ("labels", string.Join(",", call.List("labels"))), ("assignee", call.Text("assignee")), ("sort", "updated"));
            var items = await Array(call, $"/repos/{repo}/issues?{query}", call.Int("max", 30, 1, 100));
            // The issues list also returns pull requests; those have their own card.
            return GitHubShapes.List(GitHubShapes.Many(items, item => GitHubShapes.Issue(item, repo),
                item => (item as JsonObject)?["pull_request"] == null));
        }
    }

    public sealed class GitHubUpdateIssueAction : GitHubActionBase
    {
        public GitHubUpdateIssueAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubUpdateIssue;
        public override string DisplayName => "Update or close GitHub issue";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var body = new JsonObject();
            Put(body, "title", call.Text("title"));
            Put(body, "body", call.Text("body"));
            if (call.Text("state") is "open" or "closed") { body["state"] = call.Text("state"); }
            var labels = call.List("labels");
            if (labels.Count > 0) { body["labels"] = Strings(labels); }
            var assignees = call.List("assignees");
            if (assignees.Count > 0) { body["assignees"] = Strings(assignees); }
            if (body.Count == 0)
            {
                throw new GitHubSettingException("Fill in at least one thing to change.", retryable: false);
            }

            return GitHubShapes.Issue(await Object(call, new HttpMethod("PATCH"), $"/repos/{repo}/issues/{call.Number()}", body), repo);
        }
    }

    public sealed class GitHubCommentIssueAction : GitHubActionBase
    {
        public GitHubCommentIssueAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubCommentIssue;
        public override string DisplayName => "Comment on GitHub issue or pull request";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var number = call.Number();
            var json = await Object(call, HttpMethod.Post, $"/repos/{repo}/issues/{number}/comments",
                new JsonObject { ["body"] = call.Required("body", "Comment") });
            return new JsonObject { ["id"] = json["id"]?.DeepClone(), ["url"] = GitHubShapes.Text(json, "html_url"), ["number"] = number };
        }
    }

    // ---------- pull requests ----------
    public sealed class GitHubCreatePullRequestAction : GitHubActionBase
    {
        public GitHubCreatePullRequestAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubCreatePullRequest;
        public override string DisplayName => "Create GitHub pull request";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var body = new JsonObject
            {
                ["title"] = call.Required("title", "Title"),
                ["head"] = call.Required("head", "Branch with the changes"),
                ["base"] = call.Required("base", "Branch to merge into"),
                ["draft"] = call.Flag("draft")
            };
            Put(body, "body", call.Text("body"));
            return GitHubShapes.PullRequest(await Object(call, HttpMethod.Post, $"/repos/{repo}/pulls", body), repo);
        }
    }

    public sealed class GitHubGetPullRequestAction : GitHubActionBase
    {
        public GitHubGetPullRequestAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubGetPullRequest;
        public override string DisplayName => "Get GitHub pull request";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            return GitHubShapes.PullRequest(await Object(call, HttpMethod.Get, $"/repos/{repo}/pulls/{call.Number()}"), repo);
        }
    }

    public sealed class GitHubListPullRequestsAction : GitHubActionBase
    {
        public GitHubListPullRequestsAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubListPullRequests;
        public override string DisplayName => "Find GitHub pull requests";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var state = call.Text("state") is "closed" or "all" ? call.Text("state") : "open";
            var items = await Array(call, $"/repos/{repo}/pulls?{Qs(("state", state), ("base", call.Text("base")), ("sort", "updated"))}", call.Int("max", 30, 1, 100));
            return GitHubShapes.List(GitHubShapes.Many(items, item => GitHubShapes.PullRequest(item, repo)));
        }
    }

    public sealed class GitHubMergePullRequestAction : GitHubActionBase
    {
        public GitHubMergePullRequestAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubMergePullRequest;
        public override string DisplayName => "Merge GitHub pull request";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var number = call.Number();
            var method = call.Text("method") is "squash" or "rebase" ? call.Text("method") : "merge";
            var body = new JsonObject { ["merge_method"] = method };
            Put(body, "commit_title", call.Text("commitTitle"));
            var json = await Object(call, HttpMethod.Put, $"/repos/{repo}/pulls/{number}/merge", body);
            return new JsonObject
            {
                ["merged"] = json["merged"]?.GetValue<bool>() ?? false,
                ["sha"] = GitHubShapes.Text(json, "sha"),
                ["message"] = GitHubShapes.Text(json, "message"),
                ["number"] = number
            };
        }
    }

    // ---------- code ----------
    public sealed class GitHubListBranchesAction : GitHubActionBase
    {
        public GitHubListBranchesAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubListBranches;
        public override string DisplayName => "List GitHub branches";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var items = await Array(call, $"/repos/{call.Repo()}/branches", call.Int("max", 50, 1, 100));
            var names = GitHubShapes.Many(items, item => new JsonObject { ["name"] = GitHubShapes.Text(item, "name"), ["sha"] = GitHubShapes.Text(item, "commit", "sha") });
            var result = GitHubShapes.List(names);
            result["names"] = string.Join(", ", names.Select(item => item!["name"]!.ToString()));
            return result;
        }
    }

    public sealed class GitHubListCommitsAction : GitHubActionBase
    {
        public GitHubListCommitsAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubListCommits;
        public override string DisplayName => "List GitHub commits";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var branch = call.Text("branch");
            var items = await Array(call, $"/repos/{repo}/commits?{Qs(("sha", branch))}", call.Int("max", 20, 1, 100));
            return GitHubShapes.List(GitHubShapes.Many(items, item => GitHubShapes.Commit(item, repo, branch)));
        }
    }

    public sealed class GitHubCreateBranchAction : GitHubActionBase
    {
        public GitHubCreateBranchAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubCreateBranch;
        public override string DisplayName => "Create GitHub branch";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var name = call.Required("name", "New branch name");
            var from = call.Text("from");
            if (from.Length == 0)
            {
                from = GitHubShapes.Text(await Object(call, HttpMethod.Get, $"/repos/{repo}"), "default_branch");
            }

            var source = await Object(call, HttpMethod.Get, $"/repos/{repo}/git/ref/heads/{GitHubApiClient.FilePath(from)}");
            var sha = GitHubShapes.Text(source, "object", "sha");
            await Object(call, HttpMethod.Post, $"/repos/{repo}/git/refs", new JsonObject { ["ref"] = "refs/heads/" + name, ["sha"] = sha });
            return new JsonObject { ["branch"] = name, ["from"] = from, ["sha"] = sha };
        }
    }

    public sealed class GitHubGetFileAction : GitHubActionBase
    {
        private const int MaxBytes = 1024 * 1024;

        public GitHubGetFileAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubGetFile;
        public override string DisplayName => "Read GitHub file";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var path = call.Required("path", "File path");
            var json = await Api.SendAsync(call.Connection, HttpMethod.Get,
                $"/repos/{repo}/contents/{GitHubApiClient.FilePath(path)}" + (call.Text("ref").Length > 0 ? "?ref=" + Uri.EscapeDataString(call.Text("ref")) : string.Empty),
                null, call.CancellationToken);
            if (json is JsonArray folder)
            {
                var entries = GitHubShapes.Many(folder, item => new JsonObject
                {
                    ["name"] = GitHubShapes.Text(item, "name"),
                    ["path"] = GitHubShapes.Text(item, "path"),
                    ["type"] = GitHubShapes.Text(item, "type")
                });
                var listing = GitHubShapes.List(entries);
                listing["isFolder"] = true;
                return listing;
            }

            var file = json as JsonObject ?? new JsonObject();
            var size = file["size"]?.GetValue<long>() ?? 0;
            if (size > MaxBytes)
            {
                throw new GitHubSettingException("The file is bigger than 1 MB.", retryable: false);
            }

            var content = GitHubShapes.Text(file, "content").Replace("\n", string.Empty);
            string text;
            try { text = Encoding.UTF8.GetString(Convert.FromBase64String(content)); }
            catch (FormatException) { throw new GitHubSettingException("GitHub sent a file that cannot be read as text.", retryable: false); }
            return new JsonObject
            {
                ["text"] = text,
                ["sha"] = GitHubShapes.Text(file, "sha"),
                ["size"] = size,
                ["path"] = GitHubShapes.Text(file, "path"),
                ["url"] = GitHubShapes.Text(file, "html_url"),
                ["isFolder"] = false
            };
        }
    }

    public sealed class GitHubPutFileAction : GitHubActionBase
    {
        public GitHubPutFileAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubPutFile;
        public override string DisplayName => "Create or change GitHub file";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var path = call.Required("path", "File path");
            var branch = call.Text("branch");
            var url = $"/repos/{repo}/contents/{GitHubApiClient.FilePath(path)}";
            var message = call.Text("message");
            var body = new JsonObject
            {
                ["message"] = message.Length > 0 ? message : "Update " + path,
                ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(call.Text("text")))
            };
            Put(body, "branch", branch);

            // Changing an existing file needs its current sha; a new file has none.
            try
            {
                var existing = await Object(call, HttpMethod.Get, url + (branch.Length > 0 ? "?ref=" + Uri.EscapeDataString(branch) : string.Empty));
                Put(body, "sha", GitHubShapes.Text(existing, "sha"));
            }
            catch (IntegrationException ex) when (ex.Message.Contains("404"))
            {
                // new file
            }

            var json = await Object(call, HttpMethod.Put, url, body);
            return new JsonObject
            {
                ["path"] = path,
                ["commitSha"] = GitHubShapes.Text(json, "commit", "sha"),
                ["url"] = GitHubShapes.Text(json, "content", "html_url"),
                ["created"] = !body.ContainsKey("sha")
            };
        }
    }

    // ---------- releases and workflows ----------
    public sealed class GitHubCreateReleaseAction : GitHubActionBase
    {
        public GitHubCreateReleaseAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubCreateRelease;
        public override string DisplayName => "Create GitHub release";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var body = new JsonObject
            {
                ["tag_name"] = call.Required("tag", "Tag"),
                ["draft"] = call.Flag("draft"),
                ["prerelease"] = call.Flag("prerelease")
            };
            Put(body, "name", call.Text("name"));
            Put(body, "body", call.Text("body"));
            Put(body, "target_commitish", call.Text("target"));
            return GitHubShapes.Release(await Object(call, HttpMethod.Post, $"/repos/{repo}/releases", body), repo);
        }
    }

    public sealed class GitHubDispatchWorkflowAction : GitHubActionBase
    {
        public GitHubDispatchWorkflowAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubDispatchWorkflow;
        public override string DisplayName => "Run GitHub workflow";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var workflow = call.Required("workflow", "Workflow file");
            var reference = call.Text("ref");
            var body = new JsonObject { ["ref"] = reference.Length > 0 ? reference : "main" };
            var inputs = call.Text("inputs");
            if (inputs.Length > 0)
            {
                try
                {
                    if (JsonNode.Parse(inputs) is not JsonObject parsed)
                    {
                        throw new GitHubSettingException("Inputs must be a JSON object, for example {\"name\": \"value\"}.", retryable: false);
                    }

                    body["inputs"] = parsed;
                }
                catch (JsonException)
                {
                    throw new GitHubSettingException("Inputs must be a JSON object, for example {\"name\": \"value\"}.", retryable: false);
                }
            }

            await Api.SendAsync(call.Connection, HttpMethod.Post,
                $"/repos/{repo}/actions/workflows/{GitHubApiClient.Segment(workflow)}/dispatches", body, call.CancellationToken);
            return new JsonObject { ["started"] = true, ["workflow"] = workflow, ["ref"] = body["ref"]!.ToString() };
        }
    }

    public sealed class GitHubListWorkflowRunsAction : GitHubActionBase
    {
        public GitHubListWorkflowRunsAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubListWorkflowRuns;
        public override string DisplayName => "List GitHub workflow runs";
        protected override bool Writes => false;

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var repo = call.Repo();
            var items = await Array(call, $"/repos/{repo}/actions/runs?{Qs(("status", call.Text("status")), ("branch", call.Text("branch")))}", call.Int("max", 10, 1, 100));
            return GitHubShapes.List(GitHubShapes.Many(items, item => GitHubShapes.WorkflowRun(item, repo)));
        }
    }

    // ---------- anything else the GitHub API can do ----------
    public sealed class GitHubApiRequestAction : GitHubActionBase
    {
        private static readonly string[] Methods = { "GET", "POST", "PUT", "PATCH", "DELETE" };

        public GitHubApiRequestAction(GitHubApiClient api) : base(api) { }
        public override string Type => IntegrationTypeCodes.GitHubApiRequest;
        public override string DisplayName => "GitHub API request";

        protected override async Task<JsonObject> RunAsync(GitHubCall call)
        {
            var method = call.Text("method").ToUpperInvariant();
            if (!Methods.Contains(method))
            {
                method = "GET";
            }

            var path = GitHubApiClient.SafePath(call.Text("path"));
            JsonNode? body = null;
            var text = call.Text("body");
            if (text.Length > 0)
            {
                try { body = JsonNode.Parse(text); }
                catch (JsonException) { throw new GitHubSettingException("Body must be valid JSON.", retryable: false); }
            }

            var result = await Api.SendAsync(call.Connection, new HttpMethod(method), path, body, call.CancellationToken);
            return result as JsonObject ?? new JsonObject { ["items"] = result, ["count"] = (result as JsonArray)?.Count };
        }
    }
}
