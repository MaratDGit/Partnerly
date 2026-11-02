using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Scheduler;

namespace Partnerly.Automation.Tests
{
    public class CronScheduleCalculatorTests
    {
        private readonly CronScheduleCalculator _calculator = new();
        private static readonly DateTime UtcNow = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Interval_adds_minutes_in_asia_yerevan_zone_id()
        {
            var node = Node(@"{ ""scheduleKind"": ""interval"", ""minutes"": ""15"" }");
            var due = _calculator.NextDueUtc(node, UtcNow, "Asia/Yerevan");
            Assert.Equal(UtcNow.AddMinutes(15), due);
        }

        [Fact]
        public void Once_returns_future_utc_and_skips_past()
        {
            var future = Node(@"{ ""scheduleKind"": ""once"", ""at"": ""2026-11-02T18:00:00Z"" }");
            var past = Node(@"{ ""scheduleKind"": ""once"", ""at"": ""2026-11-02T11:00:00Z"" }");
            Assert.Equal(new DateTime(2026, 11, 2, 18, 0, 0, DateTimeKind.Utc), _calculator.NextDueUtc(future, UtcNow, "Asia/Yerevan"));
            Assert.Null(_calculator.NextDueUtc(past, UtcNow, "Asia/Yerevan"));
        }

        [Theory]
        [InlineData("hourly", 1, 0)]
        [InlineData("daily", 24, 0)]
        [InlineData("weekly", 168, 0)]
        public void Simple_kinds_add_fixed_offsets(string kind, int hours, int extraMinutes)
        {
            var node = Node($@"{{ ""scheduleKind"": ""{kind}"" }}");
            var due = _calculator.NextDueUtc(node, UtcNow, "Asia/Yerevan");
            Assert.Equal(UtcNow.AddHours(hours).AddMinutes(extraMinutes), due);
        }

        [Fact]
        public void Monthly_adds_one_month()
        {
            var node = Node(@"{ ""scheduleKind"": ""monthly"" }");
            Assert.Equal(UtcNow.AddMonths(1), _calculator.NextDueUtc(node, UtcNow, "Asia/Yerevan"));
        }

        [Fact]
        public void Cron_uses_asia_yerevan_local_time()
        {
            var node = Node(@"{ ""scheduleKind"": ""cron"", ""expression"": ""0 9 * * *"" }");
            var due = _calculator.NextDueUtc(node, UtcNow, "Asia/Yerevan");
            Assert.Equal(new DateTime(2026, 11, 3, 5, 0, 0, DateTimeKind.Utc), due);
        }

        private static WorkflowNode Node(string configJson)
        {
            return WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""schedule"", ""name"": ""start"", ""config"": "
                + configJson + @" }
              ]
            }").Nodes[0];
        }
    }
}
