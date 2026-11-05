namespace Partnerly.Automation.Web.ViewModels
{
    // Live view of one agent: what is running now, what ran, and when it fires next.
    // All instants are UTC (JSON carries "Z"); the browser only counts down, text dates
    // are already converted to the agent's own time zone.
    public class AutomationRunMonitorViewModel
    {
        public DateTime NowUtc { get; set; }
        public string TimeZoneId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public int ActiveRuns { get; set; }
        public int MaxConcurrentRuns { get; set; }
        public int RunsLastHour { get; set; }
        public int MaxRunsPerHour { get; set; }
        public RunMonitorNext? Next { get; set; }
        public List<string> TriggerTypes { get; set; } = new();
        public List<RunMonitorRow> Runs { get; set; } = new();
    }

    public class RunMonitorNext
    {
        public DateTime AtUtc { get; set; }
        public string AtText { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
    }

    public class RunMonitorStep
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class RunMonitorRow
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty;
        public bool IsDryRun { get; set; }
        public string StartedText { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public int CompletedSteps { get; set; }
        public int TotalSteps { get; set; }
        // One process can hold good and bad steps at once: counts of the last attempt of each card.
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Pending { get; set; }
        public List<RunMonitorStep> Steps { get; set; } = new();
        public DateTime? ResumeAtUtc { get; set; }
        public string? Error { get; set; }
    }
}
