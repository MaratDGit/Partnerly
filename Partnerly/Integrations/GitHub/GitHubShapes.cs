using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.GitHub
{
    // Turns big GitHub answers into the small flat objects that cards and triggers hand to the next steps.
    public static class GitHubShapes
    {
        private const int MaxBody = 4000;

        public static string Text(JsonNode? node, params string[] path)
        {
            var current = node;
            foreach (var key in path)
            {
                current = (current as JsonObject)?[key];
            }

            return current?.ToString() ?? string.Empty;
        }

        public static string Cut(string value, int max = MaxBody) => value.Length > max ? value[..max] : value;

        public static JsonObject Repo(JsonNode? repo) => new()
        {
            ["fullName"] = Text(repo, "full_name"),
            ["private"] = (repo as JsonObject)?["private"]?.GetValue<bool>() ?? false,
            ["description"] = Text(repo, "description"),
            ["defaultBranch"] = Text(repo, "default_branch"),
            ["url"] = Text(repo, "html_url"),
            ["cloneUrl"] = Text(repo, "clone_url"),
            ["sshUrl"] = Text(repo, "ssh_url"),
            ["stars"] = (repo as JsonObject)?["stargazers_count"]?.DeepClone(),
            ["forks"] = (repo as JsonObject)?["forks_count"]?.DeepClone(),
            ["openIssues"] = (repo as JsonObject)?["open_issues_count"]?.DeepClone(),
            ["pushedAt"] = Text(repo, "pushed_at")
        };

        public static JsonObject Issue(JsonNode? issue, string repo) => new()
        {
            ["repo"] = repo,
            ["number"] = (issue as JsonObject)?["number"]?.DeepClone(),
            ["title"] = Text(issue, "title"),
            ["body"] = Cut(Text(issue, "body")),
            ["state"] = Text(issue, "state"),
            ["author"] = Text(issue, "user", "login"),
            ["labels"] = Names((issue as JsonObject)?["labels"], "name"),
            ["assignees"] = Names((issue as JsonObject)?["assignees"], "login"),
            ["comments"] = (issue as JsonObject)?["comments"]?.DeepClone(),
            ["url"] = Text(issue, "html_url"),
            ["isPullRequest"] = (issue as JsonObject)?["pull_request"] != null,
            ["created"] = Text(issue, "created_at")
        };

        public static JsonObject PullRequest(JsonNode? pr, string repo) => new()
        {
            ["repo"] = repo,
            ["number"] = (pr as JsonObject)?["number"]?.DeepClone(),
            ["title"] = Text(pr, "title"),
            ["body"] = Cut(Text(pr, "body")),
            ["state"] = Text(pr, "state"),
            ["draft"] = (pr as JsonObject)?["draft"]?.GetValue<bool>() ?? false,
            ["merged"] = (pr as JsonObject)?["merged"]?.GetValue<bool>() ?? !string.IsNullOrEmpty(Text(pr, "merged_at")),
            ["mergeable"] = (pr as JsonObject)?["mergeable"]?.DeepClone(),
            ["head"] = Text(pr, "head", "ref"),
            ["base"] = Text(pr, "base", "ref"),
            ["author"] = Text(pr, "user", "login"),
            ["url"] = Text(pr, "html_url"),
            ["created"] = Text(pr, "created_at")
        };

        public static JsonObject Commit(JsonNode? commit, string repo, string branch) => new()
        {
            ["repo"] = repo,
            ["branch"] = branch,
            ["sha"] = Text(commit, "sha"),
            ["message"] = Cut(Text(commit, "commit", "message"), 1000),
            ["subject"] = FirstLine(Text(commit, "commit", "message")),
            ["author"] = Text(commit, "commit", "author", "name"),
            ["login"] = Text(commit, "author", "login"),
            ["date"] = Text(commit, "commit", "author", "date"),
            ["url"] = Text(commit, "html_url")
        };

        public static JsonObject Release(JsonNode? release, string repo) => new()
        {
            ["repo"] = repo,
            ["id"] = (release as JsonObject)?["id"]?.DeepClone(),
            ["tag"] = Text(release, "tag_name"),
            ["name"] = Text(release, "name"),
            ["body"] = Cut(Text(release, "body")),
            ["draft"] = (release as JsonObject)?["draft"]?.GetValue<bool>() ?? false,
            ["prerelease"] = (release as JsonObject)?["prerelease"]?.GetValue<bool>() ?? false,
            ["author"] = Text(release, "author", "login"),
            ["url"] = Text(release, "html_url"),
            ["published"] = Text(release, "published_at")
        };

        public static JsonObject WorkflowRun(JsonNode? run, string repo) => new()
        {
            ["repo"] = repo,
            ["id"] = (run as JsonObject)?["id"]?.DeepClone(),
            ["name"] = Text(run, "name"),
            ["status"] = Text(run, "status"),
            ["conclusion"] = Text(run, "conclusion"),
            ["branch"] = Text(run, "head_branch"),
            ["sha"] = Text(run, "head_sha"),
            ["commit"] = FirstLine(Text(run, "head_commit", "message")),
            ["event"] = Text(run, "event"),
            ["url"] = Text(run, "html_url"),
            ["created"] = Text(run, "created_at")
        };

        public static JsonArray Many(JsonArray source, Func<JsonNode?, JsonObject> shape, Func<JsonNode?, bool>? keep = null)
        {
            var result = new JsonArray();
            foreach (var item in source)
            {
                if (keep == null || keep(item))
                {
                    result.Add(shape(item));
                }
            }

            return result;
        }

        public static JsonObject List(JsonArray items) => new()
        {
            ["count"] = items.Count,
            ["first"] = items.Count > 0 ? items[0]!.DeepClone() : null,
            ["items"] = items
        };

        public static string FirstLine(string value)
        {
            var index = value.IndexOf('\n');
            return (index >= 0 ? value[..index] : value).Trim();
        }

        // Label / assignee lists become one text: "bug, urgent" (easy to put in a message or a filter).
        private static string Names(JsonNode? list, string key) =>
            list is JsonArray array ? string.Join(", ", array.Select(item => Text(item, key)).Where(name => name.Length > 0)) : string.Empty;
    }
}
