using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationConnectionItem
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string AutomationName { get; set; } = string.Empty;
        public string? Secret { get; set; }
    }

    public class AutomationConnectionsViewModel
    {
        public IReadOnlyList<AutomationConnectionItem> Webhooks { get; set; } = Array.Empty<AutomationConnectionItem>();
        public IReadOnlyList<AutomationConnection> HttpConnections { get; set; } = Array.Empty<AutomationConnection>();
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }
}
