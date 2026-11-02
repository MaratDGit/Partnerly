using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Partnerly.Automation.Abstractions;
using Partnerly.Descriptors.Messages;
using Partnerly.Models;

namespace Partnerly.Automation.Connectors
{
    public abstract class ConnectorBase : IEntityConnector
    {
        protected ConnectorBase(AppDbContext db)
        {
            Db = db;
        }

        protected AppDbContext Db { get; }

        public abstract string Key { get; }
        public abstract string DisplayName { get; }
        public abstract IReadOnlyList<EntityFieldDescriptor> Fields { get; }

        public abstract bool Can(string operation);
        public abstract Task<JsonObject?> FindAsync(JsonObject filters, Guid ownerUserId, CancellationToken cancellationToken);
        public abstract Task<JsonObject?> CreateAsync(JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken);
        public abstract Task<JsonObject?> UpdateAsync(JsonObject key, JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken);
        public abstract Task<bool> DeleteAsync(JsonObject key, Guid ownerUserId, CancellationToken cancellationToken);
        public abstract JsonObject ToBag(object entity);

        protected async Task<string?> OwnerRoleNameAsync(Guid ownerUserId, CancellationToken cancellationToken)
        {
            return await (
                from user in Db.Users!.AsNoTracking()
                join role in Db.Roles!.AsNoTracking() on user.RoleId equals role.Id
                where user.Id == ownerUserId
                select role.Name).FirstOrDefaultAsync(cancellationToken);
        }

        protected async Task<bool> OwnerCanViewUserAsync(Guid ownerUserId, User target, CancellationToken cancellationToken)
        {
            if (target.Id == ownerUserId)
            {
                return true;
            }

            var roleName = await OwnerRoleNameAsync(ownerUserId, cancellationToken);
            if (IsRole(roleName, Constants.SuperRoleName))
            {
                return true;
            }

            var targetRole = target.Role?.Name;
            if (string.IsNullOrWhiteSpace(targetRole) && target.RoleId != null)
            {
                targetRole = await Db.Roles!.AsNoTracking()
                    .Where(role => role.Id == target.RoleId)
                    .Select(role => role.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (IsRole(roleName, Constants.Admin) || IsRole(roleName, Constants.Employee))
            {
                return !IsRole(targetRole, Constants.Admin) && !IsRole(targetRole, Constants.SuperRoleName);
            }

            return false;
        }

        protected static bool IsRole(string? actual, string expected) =>
            string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

        protected static Guid? ReadId(JsonObject bag, params string[] names)
        {
            foreach (var name in names)
            {
                if (bag.TryGetPropertyValue(name, out var node) && Guid.TryParse(node?.ToString()?.Trim('"'), out var id))
                {
                    return id;
                }
            }

            return null;
        }

        protected static string? ReadString(JsonObject bag, string name)
        {
            return bag.TryGetPropertyValue(name, out var node) ? node?.GetValue<string>() ?? node?.ToString()?.Trim('"') : null;
        }

        protected static bool Matches(JsonObject bag, JsonObject filters)
        {
            foreach (var pair in filters)
            {
                if (string.Equals(pair.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var left = bag.TryGetPropertyValue(pair.Key, out var value) ? value?.ToString()?.Trim('"') : null;
                var right = pair.Value?.ToString()?.Trim('"');
                if (!string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        protected static JsonValue? GuidValue(Guid? value) =>
            value == null ? null : JsonValue.Create(value.Value.ToString());
    }
}
