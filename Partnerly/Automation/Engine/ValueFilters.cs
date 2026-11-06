using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Partnerly.Automation.Engine
{
    // Small functions for values in fields: {{ steps.find.result.name | upper }}, {{ vars.date | date:"dd.MM.yyyy" }},
    // {{ trigger.title | default:"no title" | truncate:40 }}. An unknown function leaves the value unchanged.
    public static class ValueFilters
    {
        public static readonly string[] Names =
        {
            "upper", "lower", "capitalize", "trim", "default", "date", "number", "round", "replace", "substring",
            "truncate", "length", "json", "first", "last", "join", "split", "add", "subtract", "multiply", "divide",
            "urlencode"
        };

        public sealed class Call
        {
            public string Name { get; init; } = string.Empty;
            public List<string> Args { get; init; } = new();
        }

        // "a.b | f:1,2 | g"  ->  path "a.b", calls [f(1,2), g()]
        public static string Split(string expression, List<Call> calls)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            char quote = '\0';
            foreach (var ch in expression)
            {
                if (quote != '\0')
                {
                    current.Append(ch);
                    if (ch == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                if (ch == '"' || ch == '\'')
                {
                    quote = ch;
                    current.Append(ch);
                    continue;
                }

                if (ch == '|')
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            parts.Add(current.ToString());
            for (var i = 1; i < parts.Count; i++)
            {
                var text = parts[i].Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                var colon = text.IndexOf(':');
                var name = (colon < 0 ? text : text[..colon]).Trim().ToLowerInvariant();
                var args = colon < 0 ? new List<string>() : SplitArgs(text[(colon + 1)..]);
                calls.Add(new Call { Name = name, Args = args });
            }

            return parts[0].Trim();
        }

        private static List<string> SplitArgs(string text)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            char quote = '\0';
            var wasQuoted = false;
            foreach (var ch in text)
            {
                if (quote != '\0')
                {
                    if (ch == quote)
                    {
                        quote = '\0';
                    }
                    else
                    {
                        current.Append(ch);
                    }

                    continue;
                }

                if (ch == '"' || ch == '\'')
                {
                    quote = ch;
                    wasQuoted = true;
                    continue;
                }

                if (ch == ',')
                {
                    result.Add(wasQuoted ? current.ToString() : current.ToString().Trim());
                    current.Clear();
                    wasQuoted = false;
                    continue;
                }

                current.Append(ch);
            }

            result.Add(wasQuoted ? current.ToString() : current.ToString().Trim());
            return result;
        }

        public static JsonNode? Apply(JsonNode? value, IEnumerable<Call> calls)
        {
            foreach (var call in calls)
            {
                value = One(value, call);
            }

            return value;
        }

        private static string Text(JsonNode? node) => VariableBinder.NodeToString(node);

        private static bool TryNumber(JsonNode? node, out double number)
        {
            if (node is JsonValue value && value.TryGetValue<double>(out number))
            {
                return true;
            }

            return double.TryParse(Text(node), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        private static string Arg(Call call, int index) => index < call.Args.Count ? call.Args[index] : string.Empty;

        private static JsonNode? One(JsonNode? value, Call call)
        {
            switch (call.Name)
            {
                case "upper": return Text(value).ToUpperInvariant();
                case "lower": return Text(value).ToLowerInvariant();
                case "trim": return Text(value).Trim();
                case "capitalize":
                {
                    var text = Text(value).Trim();
                    return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
                }
                case "default":
                    return string.IsNullOrEmpty(Text(value)) ? Arg(call, 0) : value;
                case "date":
                {
                    var format = call.Args.Count > 0 && call.Args[0].Length > 0 ? call.Args[0] : "yyyy-MM-dd";
                    return DateTime.TryParse(Text(value), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                        ? date.ToString(format, CultureInfo.InvariantCulture)
                        : Text(value);
                }
                case "number":
                {
                    var format = call.Args.Count > 0 && call.Args[0].Length > 0 ? call.Args[0] : "0.##";
                    return TryNumber(value, out var number) ? number.ToString(format, CultureInfo.InvariantCulture) : Text(value);
                }
                case "round":
                {
                    var digits = int.TryParse(Arg(call, 0), out var d) ? Math.Clamp(d, 0, 10) : 0;
                    return TryNumber(value, out var number)
                        ? Math.Round(number, digits, MidpointRounding.AwayFromZero).ToString("0.##########", CultureInfo.InvariantCulture)
                        : Text(value);
                }
                case "replace":
                    return string.IsNullOrEmpty(Arg(call, 0)) ? Text(value) : Text(value).Replace(Arg(call, 0), Arg(call, 1), StringComparison.Ordinal);
                case "substring":
                {
                    var text = Text(value);
                    var start = int.TryParse(Arg(call, 0), out var s) ? Math.Clamp(s, 0, text.Length) : 0;
                    var length = int.TryParse(Arg(call, 1), out var l) ? Math.Clamp(l, 0, text.Length - start) : text.Length - start;
                    return text.Substring(start, length);
                }
                case "truncate":
                {
                    var text = Text(value);
                    var max = int.TryParse(Arg(call, 0), out var m) && m > 0 ? m : 100;
                    return text.Length <= max ? text : text[..max] + "...";
                }
                case "length":
                    return value is JsonArray array ? array.Count : Text(value).Length;
                case "json":
                    return value?.ToJsonString() ?? "null";
                case "first":
                    return value is JsonArray f && f.Count > 0 ? JsonNodes.Clone(f[0]) : value;
                case "last":
                    return value is JsonArray l2 && l2.Count > 0 ? JsonNodes.Clone(l2[^1]) : value;
                case "join":
                    return value is JsonArray j
                        ? string.Join(call.Args.Count > 0 ? call.Args[0] : ", ", j.Select(Text))
                        : value;
                case "split":
                {
                    var separator = call.Args.Count > 0 && call.Args[0].Length > 0 ? call.Args[0] : ",";
                    var result = new JsonArray();
                    foreach (var piece in Text(value).Split(separator))
                    {
                        result.Add(piece.Trim());
                    }

                    return result;
                }
                case "add": return Math2(value, call, (a, b) => a + b);
                case "subtract": return Math2(value, call, (a, b) => a - b);
                case "multiply": return Math2(value, call, (a, b) => a * b);
                case "divide": return Math2(value, call, (a, b) => b == 0 ? a : a / b);
                case "urlencode": return Uri.EscapeDataString(Text(value));
                default: return value;
            }
        }

        private static JsonNode? Math2(JsonNode? value, Call call, Func<double, double, double> op)
        {
            if (!TryNumber(value, out var left)
                || !double.TryParse(Arg(call, 0), NumberStyles.Float, CultureInfo.InvariantCulture, out var right))
            {
                return value;
            }

            return op(left, right).ToString("0.##########", CultureInfo.InvariantCulture);
        }
    }
}
