using Partnerly.Descriptors.Messages;
using System.Globalization;

namespace Partnerly.Helpers
{
    public static class NotificationMessage
    {
        public const string Prefix = "$N:";

        private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
        {
            nameof(Messages.UserRegistrationNotification),
            nameof(Messages.NewTicketToEmployeeNotification),
            nameof(Messages.TicketStartedNotification),
            nameof(Messages.TicketClosedNotification),
            nameof(Messages.TicketReopenedNotification)
        };

        public static string Encode(string code, params object?[] args)
        {
            var stored = Prefix + code;
            if (args == null || args.Length == 0)
                return stored;

            foreach (var arg in args)
            {
                stored += "|" + (arg?.ToString() ?? string.Empty).Replace("|", " ");
            }

            return stored;
        }

        public static bool IsCoded(string? stored) =>
            !string.IsNullOrEmpty(stored) &&
            stored.StartsWith(Prefix, StringComparison.Ordinal);

        public static string Format(string? stored, CultureInfo? culture = null)
        {
            if (!TryDecode(stored, out var code, out var args))
                return stored ?? string.Empty;

            var template = Messages.Get(code, culture);
            if (args.Length == 0)
                return template;

            try
            {
                return string.Format(culture ?? CultureInfo.CurrentUICulture, template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        public static bool TryDecode(string? stored, out string code, out object[] args)
        {
            code = string.Empty;
            args = Array.Empty<object>();

            if (!IsCoded(stored))
                return false;

            var rest = stored!.Substring(Prefix.Length);
            if (string.IsNullOrEmpty(rest))
                return false;

            var parts = rest.Split('|');
            code = parts[0];
            if (!Codes.Contains(code))
                return false;

            if (parts.Length > 1)
                args = parts.Skip(1).Cast<object>().ToArray();

            return true;
        }
    }
}
