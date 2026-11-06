using System.Text.Json.Nodes;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;

namespace Partnerly.Automation.Integration
{
    public class AutomationEventBridge :
        IEventHandler<UserRegisteredEvent>,
        IEventHandler<SupportTickedCreatedEvent>,
        IEventHandler<SupportTickedStatusChangedEvent>,
        IEventHandler<AutomationAppEvent>
    {
        private readonly AutomationTriggerDispatcher _dispatcher;
        private readonly ILogger<AutomationEventBridge> _logger;

        public AutomationEventBridge(AutomationTriggerDispatcher dispatcher, ILogger<AutomationEventBridge> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        // App events published from sign-in, password and ticket flows: a failure here must never
        // break the page the user is on, so it is logged and swallowed.
        public async Task HandleAsync(AutomationAppEvent @event)
        {
            try
            {
                await _dispatcher.EnqueueEventAsync(@event.Name, @event.Payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Automation could not start agents for the app event {Event}.", @event.Name);
            }
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
