namespace Partnerly.Automation.Abstractions
{
    public static class AutomationStatuses
    {
        public const string Queued = "queued";
        public const string Running = "running";
        public const string Waiting = "waiting";
        public const string Succeeded = "succeeded";
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";

        public const string StepPending = "pending";
        public const string StepRunning = "running";
        public const string StepSucceeded = "succeeded";
        public const string StepFailed = "failed";
        public const string StepSkipped = "skipped";
        public const string StepWaiting = "waiting";

        public const string WakeupPending = "pending";
        public const string WakeupLeased = "leased";
        public const string WakeupDone = "done";
        public const string WakeupCancelled = "cancelled";

        public const string WakeupSchedule = "schedule";
        public const string WakeupInterval = "interval";
        public const string WakeupDelay = "delay";
        public const string WakeupWait = "wait";
        public const string WakeupRetry = "retry";
        public const string WakeupRecover = "recover";
        public const string WakeupStart = "start";
    }
}
