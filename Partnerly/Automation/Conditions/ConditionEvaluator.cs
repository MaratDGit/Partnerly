using System.Globalization;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Conditions
{
    public class ConditionEvaluator : IConditionEvaluator
    {
        public bool Evaluate(ConditionNode? node, JsonObject variables, IVariableBinder binder)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.Op))
            {
                return false;
            }

            var op = node.Op.Trim().ToLowerInvariant();
            return op switch
            {
                "and" => node.Children.Count > 0 && node.Children.All(child => Evaluate(child, variables, binder)),
                "or" => node.Children.Any(child => Evaluate(child, variables, binder)),
                "not" => !Evaluate(node.Children.FirstOrDefault(), variables, binder),
                "changed" => EvaluateChanged(node, variables, binder),
                "is_empty" => IsEmpty(ResolveBound(node.Left, variables, binder)),
                "is_not_empty" => !IsEmpty(ResolveBound(node.Left, variables, binder)),
                "equals" => Compare(node, variables, binder) == 0,
                "not_equals" => Compare(node, variables, binder) != 0,
                "greater_than" => Compare(node, variables, binder) > 0,
                "less_than" => Compare(node, variables, binder) < 0,
                "contains" => Contains(node, variables, binder),
                _ => false
            };
        }

        private static bool EvaluateChanged(ConditionNode node, JsonObject variables, IVariableBinder binder)
        {
            var field = binder.Interpolate(node.Left, variables);
            if (string.IsNullOrWhiteSpace(field))
            {
                return false;
            }

            var before = binder.Resolve("trigger.before." + field, variables);
            var after = binder.Resolve("trigger.after." + field, variables);
            return !JsonNodes.EqualsNode(before, after);
        }

        private static JsonNode? ResolveBound(string? value, JsonObject variables, IVariableBinder binder)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value.Contains("{{", StringComparison.Ordinal))
            {
                var interpolated = binder.Interpolate(value, variables);
                return interpolated.Length == 0 ? null : JsonValue.Create(interpolated);
            }

            if (LooksLikePath(value))
            {
                return binder.Resolve(value, variables);
            }

            return JsonValue.Create(value);
        }

        private static bool LooksLikePath(string value)
        {
            return value.Contains('.')
                || value.StartsWith("vars", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("trigger", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("steps", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("execution", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEmpty(JsonNode? node)
        {
            if (node is null)
            {
                return true;
            }

            var text = VariableBinder.NodeToString(node);
            return string.IsNullOrWhiteSpace(text) || text == "null" || text == "[]" || text == "{}";
        }

        private static bool Contains(ConditionNode node, JsonObject variables, IVariableBinder binder)
        {
            var left = VariableBinder.NodeToString(ResolveBound(node.Left, variables, binder));
            var right = VariableBinder.NodeToString(ResolveBound(node.Right, variables, binder));
            return left.Contains(right, StringComparison.OrdinalIgnoreCase);
        }

        private static int Compare(ConditionNode node, JsonObject variables, IVariableBinder binder)
        {
            var left = VariableBinder.NodeToString(ResolveBound(node.Left, variables, binder));
            var right = VariableBinder.NodeToString(ResolveBound(node.Right, variables, binder));

            if (decimal.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out var leftNumber)
                && decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rightNumber))
            {
                return leftNumber.CompareTo(rightNumber);
            }

            if (DateTime.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var leftDate)
                && DateTime.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var rightDate))
            {
                return DateTime.SpecifyKind(leftDate, DateTimeKind.Utc)
                    .CompareTo(DateTime.SpecifyKind(rightDate, DateTimeKind.Utc));
            }

            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
