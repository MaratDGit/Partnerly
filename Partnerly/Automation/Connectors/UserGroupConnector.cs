using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Connectors
{
    public class UserGroupConnector : ConnectorBase
    {
        private readonly IUserGroupService _groups;

        public UserGroupConnector(AppDbContext db, IUserGroupService groups) : base(db)
        {
            _groups = groups;
        }

        public override string Key => "user_group";
        public override string DisplayName => "User group";

        public override IReadOnlyList<EntityFieldDescriptor> Fields { get; } = new[]
        {
            new EntityFieldDescriptor { Name = "id", DataType = "guid" },
            new EntityFieldDescriptor { Name = "name", DataType = "string" }
        };

        public override bool Can(string operation) => operation == "find";

        public override async Task<JsonObject?> FindAsync(JsonObject filters, Guid ownerUserId, CancellationToken cancellationToken)
        {
            UserGroup? group = null;
            var id = ReadId(filters, "id");
            if (id != null)
            {
                group = await _groups.GetByIDAsync(id.Value);
            }
            else
            {
                var name = ReadString(filters, "name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    group = await _groups.GetByGroupNameAsync(name);
                }
            }

            if (group == null || group.IsDeleted)
            {
                return null;
            }

            var bag = ToBag(group);
            return Matches(bag, filters) ? bag : null;
        }

        public override Task<JsonObject?> CreateAsync(JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<JsonObject?>(null);

        public override Task<JsonObject?> UpdateAsync(JsonObject key, JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<JsonObject?>(null);

        public override Task<bool> DeleteAsync(JsonObject key, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public override JsonObject ToBag(object entity)
        {
            if (entity is not UserGroup group)
            {
                return new JsonObject();
            }

            return new JsonObject
            {
                ["id"] = group.Id.ToString(),
                ["name"] = group.Name
            };
        }
    }
}
