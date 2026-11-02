using System.Globalization;
using Cronos;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Helpers;

namespace Partnerly.Automation.Scheduler
{
    public class CronScheduleCalculator
    {
        public DateTime? NextDueUtc(WorkflowNode triggerNode, DateTime utcNow, string timeZoneId)
        {
            var kind = NodeConfig.GetString(triggerNode, "scheduleKind")
                       ?? NodeConfig.GetString(triggerNode, "kind")
                       ?? "once";
            var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

            return kind switch
            {
                "once" or "specific_date" => ParseOnce(triggerNode, utc),
                "interval_minutes" or "interval" => utc.AddMinutes(ReadMinutes(triggerNode)),
                "hourly" => NextAtClock(triggerNode, utc, timeZoneId, local => local.AddHours(1)),
                "daily" => NextAtClock(triggerNode, utc, timeZoneId, local => local.AddDays(1)),
                "weekly" => NextWeekly(triggerNode, utc, timeZoneId),
                "monthly" => NextAtClock(triggerNode, utc, timeZoneId, local => local.AddMonths(1)),
                "cron" => NextCron(triggerNode, utc, timeZoneId),
                _ => utc.AddMinutes(ReadMinutes(triggerNode))
            };
        }

        private static DateTime? ParseOnce(WorkflowNode node, DateTime utcNow)
        {
            var raw = NodeConfig.GetString(node, "at") ?? NodeConfig.GetString(node, "date");
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at))
            {
                var utc = at.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(at, DateTimeKind.Utc)
                    : at.ToUniversalTime();
                return utc > utcNow ? utc : null;
            }

            return null;
        }

        private static DateTime? NextCron(WorkflowNode node, DateTime utcNow, string timeZoneId)
        {
            var expression = NodeConfig.GetString(node, "expression")
                             ?? NodeConfig.GetString(node, "cron")
                             ?? NodeConfig.GetString(node, "cronExpression");
            if (string.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            var format = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 6
                ? CronFormat.IncludeSeconds
                : CronFormat.Standard;
            var cron = CronExpression.Parse(expression, format);
            return cron.GetNextOccurrence(utcNow, TimeZoneHelper.Resolve(timeZoneId));
        }

        private static DateTime? NextWeekly(WorkflowNode node, DateTime utcNow, string timeZoneId)
        {
            var weekDays = NodeConfig.GetIntArray(node, "weekDays");
            if (weekDays.Length == 0)
            {
                return NextAtClock(node, utcNow, timeZoneId, local => local.AddDays(7));
            }

            var tz = TimeZoneHelper.Resolve(timeZoneId);
            var clock = ReadClock(node) ?? TimeSpan.Zero;
            var local = TimeZoneHelper.ToLocal(utcNow, tz);
            for (var offset = 0; offset <= 7; offset++)
            {
                var candidateLocal = local.Date.AddDays(offset).Add(clock);
                if (!weekDays.Contains((int)candidateLocal.DayOfWeek))
                {
                    continue;
                }

                if (tz.IsInvalidTime(candidateLocal))
                {
                    candidateLocal = candidateLocal.AddHours(1);
                }

                var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateLocal, DateTimeKind.Unspecified), tz);
                if (utc > utcNow)
                {
                    return utc;
                }
            }

            return utcNow.AddDays(7);
        }

        private static DateTime? NextAtClock(
            WorkflowNode node,
            DateTime utcNow,
            string timeZoneId,
            Func<DateTime, DateTime> addPeriod)
        {
            var clock = ReadClock(node);
            if (clock == null)
            {
                return addPeriod(utcNow);
            }

            var tz = TimeZoneHelper.Resolve(timeZoneId);
            var local = TimeZoneHelper.ToLocal(utcNow, tz);
            var candidateLocal = local.Date.Add(clock.Value);
            if (tz.IsInvalidTime(candidateLocal))
            {
                candidateLocal = candidateLocal.AddHours(1);
            }

            var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateLocal, DateTimeKind.Unspecified), tz);
            if (utc > utcNow)
            {
                return utc;
            }

            var nextLocal = addPeriod(candidateLocal);
            if (tz.IsInvalidTime(nextLocal))
            {
                nextLocal = nextLocal.AddHours(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), tz);
        }

        private static TimeSpan? ReadClock(WorkflowNode node)
        {
            var raw = NodeConfig.GetString(node, "time");
            return TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var clock) ? clock : null;
        }

        private static int ReadMinutes(WorkflowNode node)
        {
            var raw = NodeConfig.GetString(node, "minutes") ?? "15";
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) && minutes > 0
                ? minutes
                : 15;
        }
    }
}
