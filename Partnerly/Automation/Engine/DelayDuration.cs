using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Engine
{
    public static class DelayDuration
    {
        public const string Seconds = "seconds";
        public const string Minutes = "minutes";
        public const string Hours = "hours";
        public const string Days = "days";

        public static TimeSpan FromAmount(double amount, string? unit)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0)
            {
                return TimeSpan.Zero;
            }

            TimeSpan delay = Normalize(unit) switch
            {
                Minutes => TimeSpan.FromMinutes(amount),
                Hours => TimeSpan.FromHours(amount),
                Days => TimeSpan.FromDays(amount),
                _ => TimeSpan.FromSeconds(amount)
            };

            return delay.TotalDays > 365 ? TimeSpan.FromDays(365) : delay;
        }

        public static bool TryRead(JsonElement config, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;
            if (config.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (TryFromAmount(ReadRaw(config, "amount"), ReadRaw(config, "unit"), out delay))
            {
                return true;
            }

            if (TryFromAmount(ReadRaw(config, "startAfterAmount"), ReadRaw(config, "startAfterUnit"), out delay))
            {
                return true;
            }

            return TryParseLegacy(
                ReadRaw(config, "delay") ?? ReadRaw(config, "startAfter"),
                out delay);
        }

        public static bool TryRead(
            WorkflowNode node,
            IVariableBinder binder,
            JsonObject variables,
            out TimeSpan delay)
        {
            delay = TimeSpan.Zero;
            var amount = binder.Interpolate(NodeConfig.GetString(node, "amount") ?? string.Empty, variables);
            var unit = binder.Interpolate(NodeConfig.GetString(node, "unit") ?? string.Empty, variables);
            if (TryFromAmount(amount, unit, out delay))
            {
                return true;
            }

            var startAfterAmount = binder.Interpolate(
                NodeConfig.GetString(node, "startAfterAmount") ?? string.Empty,
                variables);
            var startAfterUnit = binder.Interpolate(
                NodeConfig.GetString(node, "startAfterUnit") ?? string.Empty,
                variables);
            if (TryFromAmount(startAfterAmount, startAfterUnit, out delay))
            {
                return true;
            }

            var legacy = binder.Interpolate(
                NodeConfig.GetString(node, "delay") ?? NodeConfig.GetString(node, "startAfter") ?? string.Empty,
                variables);
            return TryParseLegacy(legacy, out delay);
        }

        public static bool TryParseLegacy(string? raw, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var value = raw.Trim();
            if (value.Contains(':', StringComparison.Ordinal)
                && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out delay))
            {
                if (delay.TotalDays > 365)
                {
                    delay = TimeSpan.FromDays(365);
                }

                return true;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                delay = FromAmount(seconds, Seconds);
                return true;
            }

            return false;
        }

        private static bool TryFromAmount(string? amountRaw, string? unit, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(amountRaw))
            {
                return false;
            }

            if (!double.TryParse(amountRaw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
            {
                return false;
            }

            delay = FromAmount(amount, string.IsNullOrWhiteSpace(unit) ? Seconds : unit);
            return true;
        }

        private static string Normalize(string? unit)
        {
            var value = (unit ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "s" or "sec" or "second" or Seconds => Seconds,
                "m" or "min" or "minute" or Minutes => Minutes,
                "h" or "hr" or "hour" or Hours => Hours,
                "d" or "day" or Days => Days,
                _ => Seconds
            };
        }

        private static string? ReadRaw(JsonElement config, string name)
        {
            if (!config.TryGetProperty(name, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
                _ => null
            };
        }
    }
}
