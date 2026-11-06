using System.Text.Json.Nodes;
using Partnerly.Events.BaseEvents;
using Partnerly.Models;

namespace Partnerly.Events
{
    // One carrier for the "App event" trigger of agents: the name is what an agent picks in the
    // trigger card, the payload is what it can read as {{trigger.*}}. Only the automation bridge
    // listens to it, so publishing it never changes what the application itself does.
    public class AutomationAppEvent : IEvent
    {
        public const string UserLoggedIn = nameof(UserLoggedIn);
        public const string UserLoginFailed = nameof(UserLoginFailed);
        public const string UserLockedOut = nameof(UserLockedOut);
        public const string UserEmailConfirmed = nameof(UserEmailConfirmed);
        public const string PasswordResetRequested = nameof(PasswordResetRequested);
        public const string PasswordChanged = nameof(PasswordChanged);
        public const string UserDeleted = nameof(UserDeleted);
        public const string SupportTicketAssigned = nameof(SupportTicketAssigned);
        public const string SupportTicketDeleted = nameof(SupportTicketDeleted);

        public string Name { get; }
        public JsonObject Payload { get; }

        public AutomationAppEvent(string name, JsonObject payload)
        {
            Name = name;
            Payload = payload;
        }

        public static AutomationAppEvent ForUser(string name, User user, Action<JsonObject>? extra = null)
        {
            var payload = new JsonObject
            {
                ["id"] = user.Id.ToString(),
                ["email"] = user.Email,
                ["firstName"] = user.FirstName,
                ["lastName"] = user.LastName
            };
            extra?.Invoke(payload);
            return new AutomationAppEvent(name, payload);
        }

        public static AutomationAppEvent LoginFailed(User user, bool lockedOut) =>
            ForUser(UserLoginFailed, user, payload =>
            {
                payload["attempts"] = user.AccessFailedCount;
                payload["lockedOut"] = lockedOut;
            });

        public static AutomationAppEvent LockedOut(User user) =>
            ForUser(UserLockedOut, user, payload =>
            {
                payload["lockedUntil"] = user.LockoutEnd?.ToString("O");
            });

        public static AutomationAppEvent TicketAssigned(SupportTicket ticket, Guid? previousAssignedTo) =>
            new(SupportTicketAssigned, new JsonObject
            {
                ["id"] = ticket.Id.ToString(),
                ["ticketId"] = ticket.TicketID,
                ["subject"] = ticket.Subject,
                ["status"] = ticket.Status,
                ["assignedTo"] = ticket.AssignedTo?.ToString(),
                ["previousAssignedTo"] = previousAssignedTo?.ToString()
            });

        public static AutomationAppEvent TicketDeleted(SupportTicket ticket) =>
            new(SupportTicketDeleted, new JsonObject
            {
                ["id"] = ticket.Id.ToString(),
                ["ticketId"] = ticket.TicketID,
                ["subject"] = ticket.Subject,
                ["status"] = ticket.Status
            });
    }
}
