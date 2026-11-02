using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Connectors
{
    public class NotificationConnector : ConnectorBase
    {
        private readonly INotificationService _notifications;

        public NotificationConnector(AppDbContext db, INotificationService notifications) : base(db)
        {
            _notifications = notifications;
        }

        public override string Key => "notification";
        public override string DisplayName => "Notification";

        public override IReadOnlyList<EntityFieldDescriptor> Fields { get; } = new[]
        {
            new EntityFieldDescriptor { Name = "id", DataType = "guid" },
            new EntityFieldDescriptor { Name = "type", DataType = "string" },
            new EntityFieldDescriptor { Name = "message", DataType = "string" },
            new EntityFieldDescriptor { Name = "userId", DataType = "guid" },
            new EntityFieldDescriptor { Name = "link", DataType = "string" }
        };

        public override bool Can(string operation) =>
            operation is "find" or "create";

        public override async Task<JsonObject?> FindAsync(JsonObject filters, Guid ownerUserId, CancellationToken cancellationToken)
        {
            var id = ReadId(filters, "id");
            Notification? notification = id == null ? null : await _notifications.GetNotificationByIDAsync(id);
            if (notification == null || notification.UserId != ownerUserId)
            {
                return null;
            }

            var bag = ToBag(notification);
            return Matches(bag, filters) ? bag : null;
        }

        public override async Task<JsonObject?> CreateAsync(JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken)
        {
            var userId = ReadId(fields, "userId") ?? ownerUserId;
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = ReadString(fields, "type") ?? "System",
                Message = ReadString(fields, "message") ?? string.Empty,
                Link = ReadString(fields, "link")
            };
            var result = await _notifications.CreateNotificationAsync(notification);
            return result.Success && result.Data != null ? ToBag(result.Data) : null;
        }

        public override Task<JsonObject?> UpdateAsync(JsonObject key, JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<JsonObject?>(null);

        public override Task<bool> DeleteAsync(JsonObject key, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public override JsonObject ToBag(object entity)
        {
            if (entity is not Notification notification)
            {
                return new JsonObject();
            }

            return new JsonObject
            {
                ["id"] = notification.Id.ToString(),
                ["type"] = notification.Type,
                ["message"] = notification.Message,
                ["userId"] = notification.UserId.ToString(),
                ["link"] = notification.Link
            };
        }
    }
}
