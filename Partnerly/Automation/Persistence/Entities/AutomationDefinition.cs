namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationDefinition
    {
        public Guid Id { get; set; }
        public Guid OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public Guid? PublishedVersionId { get; set; }
        public Guid? DraftVersionId { get; set; }
        public string TimeZoneId { get; set; } = "Asia/Yerevan";
        public int MaxConcurrentRuns { get; set; } = 1;
        public int MaxRunsPerHour { get; set; } = 60;
        public int StepTimeoutSeconds { get; set; } = 60;
        public int RunTimeoutSeconds { get; set; } = 3600;
        public int RetryLimit { get; set; } = 3;
        public string? Note { get; set; }

        // Labels for finding agents on the hub: comma separated, e.g. "sales,weekly".
        public string? Tags { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public IReadOnlyList<string> TagList =>
            string.IsNullOrWhiteSpace(Tags)
                ? Array.Empty<string>()
                : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public static string? NormalizeTags(string? raw)
        {
            var tags = (raw ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => tag.Length > 30 ? tag.Substring(0, 30) : tag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();
            return tags.Count == 0 ? null : string.Join(",", tags);
        }

        // The icon is kept inside Note as "icon:<class>" so no new column (and migration) is needed.
        // Note is not used for anything else on a definition.
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Icon
        {
            get => Note != null && Note.StartsWith("icon:", StringComparison.Ordinal)
                ? Abstractions.AutomationIcons.Normalize(Note.Substring(5))
                : Abstractions.AutomationIcons.Default;
            set => Note = "icon:" + Abstractions.AutomationIcons.Normalize(value);
        }
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
