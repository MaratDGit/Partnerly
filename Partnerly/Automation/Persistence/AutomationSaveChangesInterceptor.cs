using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Integration;
using Partnerly.Models;

namespace Partnerly.Automation.Persistence
{
    public class AutomationSaveChangesInterceptor : IAutomationChangeSink
    {
        private readonly IServiceProvider _services;

        public AutomationSaveChangesInterceptor(IServiceProvider services)
        {
            _services = services;
        }

        public IReadOnlyList<AutomationEntityChange> Capture(ChangeTracker tracker)
        {
            if (RecursionGuard.Current > 0)
            {
                return Array.Empty<AutomationEntityChange>();
            }

            var changes = new List<AutomationEntityChange>();
            foreach (var entry in tracker.Entries())
            {
                if (entry.Entity is User user)
                {
                    var change = CaptureUser(entry, user);
                    if (change != null)
                    {
                        changes.Add(change);
                    }
                }
                else if (entry.Entity is SupportTicket ticket)
                {
                    var change = CaptureTicket(entry, ticket);
                    if (change != null)
                    {
                        changes.Add(change);
                    }
                }
            }

            return changes;
        }

        public async Task PublishAsync(IReadOnlyList<AutomationEntityChange> changes, CancellationToken cancellationToken = default)
        {
            if (changes.Count == 0)
            {
                return;
            }

            var dispatcher = (AutomationTriggerDispatcher)_services.GetRequiredService(typeof(AutomationTriggerDispatcher));
            foreach (var change in changes)
            {
                var payload = new JsonObject
                {
                    ["entity"] = change.EntityKey,
                    ["id"] = change.EntityId.ToString(),
                    ["after"] = JsonNodes.Clone(change.After),
                    ["before"] = change.Before == null ? null : JsonNodes.Clone(change.Before)
                };
                var type = change.Operation switch
                {
                    "created" => AutomationTypeCodes.EntityCreated,
                    "deleted" => AutomationTypeCodes.EntityDeleted,
                    _ => AutomationTypeCodes.EntityUpdated
                };
                await dispatcher.EnqueueEntityAsync(type, change.EntityKey, payload, cancellationToken);
            }
        }

        private static AutomationEntityChange? CaptureUser(EntityEntry entry, User user)
        {
            if (entry.State == EntityState.Added)
            {
                return new AutomationEntityChange
                {
                    EntityKey = "user",
                    Operation = "created",
                    EntityId = user.Id,
                    After = UserBag(user)
                };
            }

            if (entry.State == EntityState.Modified)
            {
                return new AutomationEntityChange
                {
                    EntityKey = "user",
                    Operation = "updated",
                    EntityId = user.Id,
                    Before = UserBagFromOriginal(entry),
                    After = UserBag(user)
                };
            }

            return null;
        }

        private static AutomationEntityChange? CaptureTicket(EntityEntry entry, SupportTicket ticket)
        {
            if (entry.State == EntityState.Added)
            {
                return new AutomationEntityChange
                {
                    EntityKey = "support_ticket",
                    Operation = "created",
                    EntityId = ticket.Id,
                    After = TicketBag(ticket)
                };
            }

            if (entry.State == EntityState.Deleted)
            {
                return new AutomationEntityChange
                {
                    EntityKey = "support_ticket",
                    Operation = "deleted",
                    EntityId = ticket.Id,
                    After = TicketBag(ticket)
                };
            }

            if (entry.State != EntityState.Modified)
            {
                return null;
            }

            var wasDeleted = OriginalBool(entry, nameof(SupportTicket.IsDeleted));
            var deleted = ticket.IsDeleted && !wasDeleted;
            return new AutomationEntityChange
            {
                EntityKey = "support_ticket",
                Operation = deleted ? "deleted" : "updated",
                EntityId = ticket.Id,
                Before = TicketBagFromOriginal(entry),
                After = TicketBag(ticket)
            };
        }

        private static JsonObject UserBag(User user) => new()
        {
            ["id"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["firstName"] = user.FirstName,
            ["lastName"] = user.LastName,
            ["status"] = user.IsBlocked == true ? "blocked" : "active"
        };

        private static JsonObject TicketBag(SupportTicket ticket) => new()
        {
            ["id"] = ticket.Id.ToString(),
            ["ticketId"] = ticket.TicketID,
            ["subject"] = ticket.Subject,
            ["status"] = ticket.Status,
            ["userId"] = ticket.UserId?.ToString(),
            ["assignedTo"] = ticket.AssignedTo?.ToString(),
            ["note"] = ticket.Note
        };

        private static JsonObject UserBagFromOriginal(EntityEntry entry) => new()
        {
            ["id"] = OriginalString(entry, nameof(User.Id)),
            ["email"] = OriginalString(entry, nameof(User.Email)),
            ["firstName"] = OriginalString(entry, nameof(User.FirstName)),
            ["lastName"] = OriginalString(entry, nameof(User.LastName))
        };

        private static JsonObject TicketBagFromOriginal(EntityEntry entry) => new()
        {
            ["id"] = OriginalString(entry, nameof(SupportTicket.Id)),
            ["ticketId"] = OriginalString(entry, nameof(SupportTicket.TicketID)),
            ["subject"] = OriginalString(entry, nameof(SupportTicket.Subject)),
            ["status"] = OriginalString(entry, nameof(SupportTicket.Status)),
            ["userId"] = OriginalString(entry, nameof(SupportTicket.UserId)),
            ["assignedTo"] = OriginalString(entry, nameof(SupportTicket.AssignedTo)),
            ["note"] = OriginalString(entry, nameof(SupportTicket.Note))
        };

        private static string? OriginalString(EntityEntry entry, string name)
        {
            var value = entry.OriginalValues[name];
            return value?.ToString();
        }

        private static bool OriginalBool(EntityEntry entry, string name)
        {
            var value = entry.OriginalValues[name];
            return value is bool flag && flag;
        }
    }
}
