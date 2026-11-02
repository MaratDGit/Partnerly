namespace Partnerly.Automation.Authoring
{
    public sealed class AutomationPreset
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string GraphJson { get; init; } = "{}";
    }

    public static class AutomationTemplateCatalog
    {
        public static IReadOnlyList<AutomationPreset> All { get; } = new[]
        {
            new AutomationPreset
            {
                Id = "manual-flag",
                Name = "Manual flag",
                Description = "Run by hand and set a variable.",
                GraphJson = @"{
                  ""schemaVersion"": 1,
                  ""nodes"": [
                    { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                    { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""flag"", ""config"": { ""name"": ""ready"", ""value"": ""yes"" } }
                  ],
                  ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
                }"
            },
            new AutomationPreset
            {
                Id = "delay-then-flag",
                Name = "Delay then flag",
                Description = "Wait five seconds, then set a variable.",
                GraphJson = @"{
                  ""schemaVersion"": 1,
                  ""nodes"": [
                    { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                    { ""id"": ""d1"", ""kind"": ""action"", ""type"": ""delay"", ""name"": ""pause"", ""config"": { ""amount"": 5, ""unit"": ""seconds"" } },
                    { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""after"", ""config"": { ""name"": ""done"", ""value"": ""1"" } }
                  ],
                  ""edges"": [
                    { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""d1"" },
                    { ""id"": ""e2"", ""from"": ""d1"", ""to"": ""a1"" }
                  ]
                }"
            },
            new AutomationPreset
            {
                Id = "daily-marker",
                Name = "Daily marker",
                Description = "Every day at 09:00 in the automation time zone.",
                GraphJson = @"{
                  ""schemaVersion"": 1,
                  ""nodes"": [
                    { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""schedule"", ""name"": ""morning"", ""config"": { ""scheduleKind"": ""daily"", ""time"": ""09:00"" } },
                    { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""tick"", ""config"": { ""name"": ""ran"", ""value"": ""1"" } }
                  ],
                  ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
                }"
            }
        };

        public static AutomationPreset? Get(string? id) =>
            All.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
