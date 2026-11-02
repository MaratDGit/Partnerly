using System.Globalization;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Engine
{
    public static class ExecutionContextFactory
    {
        public static JsonObject Create(AutomationRun run, TriggerContext? trigger, DateTime utcNow)
        {
            var nowMinus = new JsonObject();
            foreach (var days in new[] { 1, 7, 14, 30, 90 })
            {
                nowMinus[days.ToString(CultureInfo.InvariantCulture)] =
                    utcNow.AddDays(-days).ToString("O", CultureInfo.InvariantCulture);
            }

            var root = new JsonObject
            {
                ["trigger"] = JsonNodes.Clone(trigger?.Payload) as JsonObject ?? new JsonObject
                {
                    ["type"] = trigger?.Type ?? run.TriggerType
                },
                ["execution"] = new JsonObject
                {
                    ["startedAt"] = utcNow.ToString("O", CultureInfo.InvariantCulture),
                    ["runId"] = run.Id.ToString(),
                    ["dryRun"] = run.IsDryRun,
                    ["nowMinusDays"] = nowMinus
                },
                ["steps"] = new JsonObject(),
                ["vars"] = new JsonObject()
            };

            return root;
        }
    }
}
