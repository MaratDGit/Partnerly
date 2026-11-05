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

        public static readonly string[] All = { Telegram, Google, WhatsApp, Microsoft };

        public static bool IsKnown(string? kind) => kind != null && All.Contains(kind);
    }
}
