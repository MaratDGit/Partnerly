namespace Partnerly.Automation.Abstractions
{
    // Icons an agent can wear in the Agents grid (Boxicons class names). Only these are accepted,
    // so the value is safe to put into a class attribute.
    public static class AutomationIcons
    {
        public const string Default = "bx-bot";

        public static readonly string[] All =
        {
            "bx-bot", "bx-rocket", "bx-bolt-circle", "bx-bell", "bx-mail-send", "bx-envelope", "bx-message-dots", "bx-chat",
            "bx-phone", "bx-calendar", "bx-time", "bx-alarm", "bx-cart", "bx-wallet", "bx-credit-card", "bx-line-chart",
            "bx-bar-chart-alt-2", "bx-spreadsheet", "bx-file", "bx-folder", "bx-cloud", "bx-data", "bx-user", "bx-group",
            "bx-support", "bx-shield", "bx-lock", "bx-star", "bx-heart", "bx-home", "bx-globe", "bx-link-alt",
            "bx-cog", "bx-search", "bx-sync", "bx-flag", "bxl-telegram", "bxl-gmail", "bxl-whatsapp", "bxl-google"
        };

        public static string Normalize(string? icon)
        {
            var value = (icon ?? string.Empty).Trim();
            return All.Contains(value, StringComparer.Ordinal) ? value : Default;
        }
    }
}
