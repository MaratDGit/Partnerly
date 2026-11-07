namespace Partnerly.Integrations.Common
{
    // AutomationConnection.Kind values owned by Partnerly/Integrations.
    public static class IntegrationKinds
    {
        public const string Telegram = "telegram";
        public const string Google = "google";
        public const string WhatsApp = "whatsapp";
        // Outlook mail / calendar through Microsoft Graph.
        public const string Microsoft = "microsoft";

        // GitHub account: issues, pull requests, files, releases and workflows through the REST API.
        public const string GitHub = "github";

        // The user's own PC, reached through the Partnerly Agent program (Partnerly/Computer).
        public const string Computer = "computer";

        public static readonly string[] All = { Telegram, Google, WhatsApp, Microsoft, GitHub, Computer };

        public static bool IsKnown(string? kind) => kind != null && All.Contains(kind);
    }
}
