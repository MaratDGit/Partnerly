using System.Text.Json.Nodes;

namespace Partnerly.Automation.Abstractions
{
    public class StepResult
    {
        public string Status { get; set; } = AutomationStatuses.StepSucceeded;
        public JsonNode? Output { get; set; }
        public string? Error { get; set; }
        public string? Port { get; set; }
        public DateTime? ResumeAtUtc { get; set; }
        public string? WakeupKind { get; set; }
        public bool Retryable { get; set; } = true;

        public static StepResult Succeeded(JsonNode? output = null, string? port = null) =>
            new()
            {
                Status = AutomationStatuses.StepSucceeded,
                Output = output,
                Port = port
            };

        public static StepResult Skipped(string reason) =>
            new()
            {
                Status = AutomationStatuses.StepSkipped,
                Output = new JsonObject { ["skipped"] = reason }
            };

        public static StepResult Failed(string error, bool retryable = true) =>
            new()
            {
                Status = AutomationStatuses.StepFailed,
                Error = error,
                Retryable = retryable
            };

        public static StepResult Waiting(DateTime resumeAtUtc, string kind, JsonNode? output = null) =>
            new()
            {
                Status = AutomationStatuses.StepWaiting,
                ResumeAtUtc = resumeAtUtc,
                WakeupKind = kind,
                Output = output
            };
    }
}
