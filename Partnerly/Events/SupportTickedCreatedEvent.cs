using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Events
{
    public class SupportTickedCreatedEvent : IEvent
    {
        public SupportTicket? NewTicket { get; }
        public bool SendEmail { get; }
        public bool SendNotification { get; }

        public SupportTickedCreatedEvent(SupportTicket newTicket, bool sendEmail = true, bool sendNote = true)
        {
            NewTicket = newTicket;
            SendEmail = sendEmail;
            SendNotification = sendNote;
        }
    }

    public class SupportTickedCreatedEventHandler : IEventHandler<SupportTickedCreatedEvent>
    {
        private readonly IConfiguration _config;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogService _logService;

        public SupportTickedCreatedEventHandler(IConfiguration config, INotificationService notificationService, IEmailSender emailSender, ILogService logService)
        {
            _config = config;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logService = logService;
        }

        public async Task HandleAsync(SupportTickedCreatedEvent @event)
        {
            if (@event.NewTicket?.AssignedTo != null)
            {
                if (@event.SendEmail == true)
                {

                }

                if (@event.SendNotification == true)
                {
                    var appUrl = _config["AppSettings:BaseUrl"];
                    var confirmationLink = $"{appUrl}/SupportTickets/ViewCase/{@event.NewTicket.Id}";

                    var message = string.Format(Messages.NewTickedToEmployeeNotification, @event.NewTicket.TicketID);
                    Notification notification = new Notification { UserId = (Guid)@event.NewTicket.AssignedTo, Type = NotificationTypeAttribute.Warning, Message = message, Link = confirmationLink };

                    await _notificationService.CreateNotificationAsync(notification);
                }

                await _logService.CreateLogAsync(LogActionsAttribute.SupportTicketCreated, LogTypeAttribute.Information, "Ticked ID - " + @event.NewTicket.TicketID);
            }
        }
    }
}
