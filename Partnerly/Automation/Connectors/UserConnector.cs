using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Partnerly.Automation.Abstractions;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Connectors
{
    public class UserConnector : ConnectorBase
    {
        private readonly IUserService _users;

        public UserConnector(AppDbContext db, IUserService users) : base(db)
        {
            _users = users;
        }

        public override string Key => "user";
        public override string DisplayName => "User";

        public override IReadOnlyList<EntityFieldDescriptor> Fields { get; } = new[]
        {
            new EntityFieldDescriptor { Name = "id", DataType = "guid" },
            new EntityFieldDescriptor { Name = "email", DataType = "string" },
            new EntityFieldDescriptor { Name = "firstName", DataType = "string", Updatable = true },
            new EntityFieldDescriptor { Name = "lastName", DataType = "string", Updatable = true },
            new EntityFieldDescriptor { Name = "lastActivity", DataType = "datetime" },
            new EntityFieldDescriptor { Name = "isBlocked", DataType = "bool", Updatable = true },
            new EntityFieldDescriptor { Name = "emailConfirmed", DataType = "bool" },
            new EntityFieldDescriptor { Name = "roleId", DataType = "guid" },
            new EntityFieldDescriptor { Name = "myReferralCode", DataType = "string" },
            new EntityFieldDescriptor { Name = "referrerId", DataType = "guid" },
            new EntityFieldDescriptor { Name = "isOnlayn", DataType = "bool" }
        };

        public override bool Can(string operation) =>
            operation is "find" or "update";

        public override async Task<JsonObject?> FindAsync(JsonObject filters, Guid ownerUserId, CancellationToken cancellationToken)
        {
            User? user = null;
            var id = ReadId(filters, "id");
            if (id != null)
            {
                user = await _users.GetUserByIDAsync(id);
            }
            else
            {
                var email = ReadString(filters, "email");
                if (!string.IsNullOrWhiteSpace(email))
                {
                    user = await _users.GetUserByEmailAsync(email);
                }
            }

            if (user == null || !await OwnerCanViewUserAsync(ownerUserId, user, cancellationToken))
            {
                return null;
            }

            var bag = ToBag(user);
            return Matches(bag, filters) ? bag : null;
        }

        public override Task<JsonObject?> CreateAsync(JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<JsonObject?>(null);

        public override async Task<JsonObject?> UpdateAsync(JsonObject key, JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken)
        {
            var id = ReadId(key, "id") ?? ReadId(fields, "id");
            if (id == null)
            {
                return null;
            }

            var user = await _users.GetUserByIDAsync(id);
            if (user == null || !await OwnerCanViewUserAsync(ownerUserId, user, cancellationToken))
            {
                return null;
            }

            var roleName = await OwnerRoleNameAsync(ownerUserId, cancellationToken);
            if (fields.TryGetPropertyValue("firstName", out var first))
            {
                user.FirstName = first?.GetValue<string>() ?? user.FirstName;
            }

            if (fields.TryGetPropertyValue("lastName", out var last))
            {
                user.LastName = last?.GetValue<string>() ?? user.LastName;
            }

            if (fields.TryGetPropertyValue("isBlocked", out var blocked)
                && !IsRole(roleName, Constants.User)
                && blocked != null)
            {
                user.IsBlocked = blocked.GetValue<bool>();
            }

            var result = await _users.UpdateUserAsync(user, ownerUserId);
            return result.Success && result.Data != null ? ToBag(result.Data) : null;
        }

        public override Task<bool> DeleteAsync(JsonObject key, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public override JsonObject ToBag(object entity)
        {
            if (entity is not User user)
            {
                return new JsonObject();
            }

            return new JsonObject
            {
                ["id"] = user.Id.ToString(),
                ["email"] = user.Email,
                ["firstName"] = user.FirstName,
                ["lastName"] = user.LastName,
                ["lastActivity"] = user.LastActivity?.ToUniversalTime().ToString("O"),
                ["isBlocked"] = user.IsBlocked,
                ["emailConfirmed"] = user.EmailConfirmed,
                ["roleId"] = GuidValue(user.RoleId),
                ["myReferralCode"] = user.MyReferralCode,
                ["referrerId"] = GuidValue(user.ReferrerId),
                ["isOnlayn"] = user.IsOnlayn
            };
        }
    }
}
