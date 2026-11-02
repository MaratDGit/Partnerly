using System.Text.Json.Nodes;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;

namespace Partnerly.Automation.Integration
{
    public class AutomationEventBridge :
        IEventHandler<UserRegisteredEvent>,
        IEventHandler<SupportTickedCreatedEvent>,
        IEventHandler<SupportTickedStatusChangedEvent>
    {
        private readonly AutomationTriggerDispatcher _dispatcher;

        public AutomationEventBridge(AutomationTriggerDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public Task HandleAsync(UserRegisteredEvent @event)
        {
            var user = @event.NewUser;
            return _dispatcher.EnqueueEventAsync("UserRegistered", new JsonObject
            {
                ["id"] = user.Id.ToString(),
                ["email"] = user.Email,
                ["firstName"] = user.FirstName,
                ["lastName"] = user.LastName
            });
        }

        public Task HandleAsync(SupportTickedCreatedEvent @event)
        {
            var ticket = @event.NewTicket;
            if (ticket == null)
            {
                return Task.CompletedTask;
            }

            return _dispatcher.EnqueueEventAsync("SupportTicketCreated", new JsonObject
            {
                ["id"] = ticket.Id.ToString(),
                ["ticketId"] = ticket.TicketID,
                ["subject"] = ticket.Subject,
                ["status"] = ticket.Status,
                ["type"] = @event.Type.ToString()
            });
        }

        public Task HandleAsync(SupportTickedStatusChangedEvent @event)
        {
            var ticket = @event.Ticket;
            if (ticket == null)
            {
                return Task.CompletedTask;
            }

            return _dispatcher.EnqueueEventAsync("SupportTicketStatusChanged", new JsonObject
            {
                ["id"] = ticket.Id.ToString(),
                ["ticketId"] = ticket.TicketID,
                ["status"] = ticket.Status
            });
        }
    }
}
