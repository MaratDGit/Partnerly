using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Partnerly.Automation.Integration
{
    public interface IAutomationChangeSink
    {
        IReadOnlyList<AutomationEntityChange> Capture(ChangeTracker tracker);
        Task PublishAsync(IReadOnlyList<AutomationEntityChange> changes, CancellationToken cancellationToken = default);
    }

    public class AutomationEntityChange
    {
        public string EntityKey { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public System.Text.Json.Nodes.JsonObject After { get; set; } = new();
        public System.Text.Json.Nodes.JsonObject? Before { get; set; }
    }
}
