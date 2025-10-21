using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Events
{
    public class SupportTickedStatusChangedEvent : IEvent
    {
        public string? OldStatus { get; }
        public SupportTicket? Ticket { get; }
        public bool SendEmail { get; }
        public bool SendNotification { get; }

        public SupportTickedStatusChangedEvent(string? oldStatus, SupportTicket? ticket, bool sendEmail = true, bool sendNote = true)
        {
            OldStatus = oldStatus;
            Ticket = ticket;
            SendEmail = sendEmail;
            SendNotification = sendNote;
        }
    }

    public class SupportTickedStatusChangedEventHandler : IEventHandler<SupportTickedStatusChangedEvent>
    {
        private readonly IConfiguration _config;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogService _logService;

        public SupportTickedStatusChangedEventHandler(IConfiguration config, INotificationService notificationService, IEmailSender emailSender, ILogService logService)
        {
            _config = config;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logService = logService;
        }

        public async Task HandleAsync(SupportTickedStatusChangedEvent @event)
        {
            if (@event.Ticket != null && @event.OldStatus != null && @event.OldStatus != @event.Ticket.Status)
            {
                if (@event.SendEmail == true)
                {

                }
                if (@event.SendNotification == true)
                {
                    string status = @event.Ticket.Status;
                    string? message = null;
                    if (status == SupportTicketStatusAttribute.New)
                    {
                        message = string.Format(Messages.TickedReopenedNotification, @event.Ticket.TicketID);
                    }
                    else if (status == SupportTicketStatusAttribute.InProgress)
                    {
                        message = string.Format(Messages.TickedStartedNotification, @event.Ticket.TicketID);
                    }
                    else if (status == SupportTicketStatusAttribute.Closed)
                    {
                        message = string.Format(Messages.TickedClosedNotification, @event.Ticket.TicketID);
                    }

                    if (message != null)
                    {
                        var appUrl = _config["AppSettings:BaseUrl"];
                        var confirmationLink = $"{appUrl}/SupportTickets/ViewCase/{@event.Ticket.Id}";

                        Notification notification = new Notification { UserId = (Guid)@event.Ticket.UserId, Type = NotificationTypeAttribute.Information, Message = message, Link = confirmationLink };
                        await _notificationService.CreateNotificationAsync(notification);

                        if (status == SupportTicketStatusAttribute.Closed)
                        {
                            var employeeNote = await _notificationService.GetNotificationByTicketIDAsync(@event.Ticket.Id);
                            if (employeeNote != null)
                            {
                                employeeNote.IsRead = true;
                                await _notificationService.UpdateNotificationAsync(employeeNote);
                            }
                        }
                        else if (status == SupportTicketStatusAttribute.New)
                        {
                            var employeeNote = await _notificationService.GetNotificationByTicketIDAsync(@event.Ticket.Id);
                            if (employeeNote != null && employeeNote.IsRead == true)
                            {
                                employeeNote.IsRead = false;
                                await _notificationService.UpdateNotificationAsync(employeeNote);
                            }
                        }
                    }
                }
            }
        }
    }
}
