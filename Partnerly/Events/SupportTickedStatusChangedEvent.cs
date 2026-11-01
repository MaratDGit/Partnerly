using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
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
        private readonly INotificationService _notificationService;
        private readonly ILogService _logService;

        public SupportTickedStatusChangedEventHandler(
            IConfiguration config,
            INotificationService notificationService,
            IEmailSender emailSender,
            ILogService logService)
        {
            _notificationService = notificationService;
            _logService = logService;
        }

        public async Task HandleAsync(SupportTickedStatusChangedEvent @event)
        {
            if (@event.Ticket == null ||
                string.IsNullOrEmpty(@event.OldStatus) ||
                @event.OldStatus == @event.Ticket.Status ||
                @event.SendNotification != true)
            {
                return;
            }

            var status = @event.Ticket.Status;
            string? message = status switch
            {
                SupportTicketStatusAttribute.New =>
                    NotificationMessage.Encode(
                        nameof(Messages.TicketReopenedNotification),
                        @event.Ticket.TicketID),
                SupportTicketStatusAttribute.InProgress =>
                    NotificationMessage.Encode(
                        nameof(Messages.TicketStartedNotification),
                        @event.Ticket.TicketID),
                SupportTicketStatusAttribute.Closed =>
                    NotificationMessage.Encode(
                        nameof(Messages.TicketClosedNotification),
                        @event.Ticket.TicketID),
                _ => null
            };

            if (message == null)
                return;

            var recipientId = @event.Ticket.UserId ?? @event.Ticket.CreatedBy;
            if (recipientId == null || recipientId == Guid.Empty)
            {
                await _logService.CreateLogAsync(
                    LogActionsAttribute.SupportTicketUpdated,
                    LogTypeAttribute.Error,
                    "Ticket status notification skipped: creator UserId is empty. Ticket " + @event.Ticket.TicketID);
                return;
            }

            try
            {
                var confirmationLink = $"/SupportTickets/ViewCase/{@event.Ticket.Id}";
                var notification = new Notification
                {
                    UserId = recipientId.Value,
                    Type = NotificationTypeAttribute.Information,
                    Message = message,
                    Link = confirmationLink
                };

                var created = await _notificationService.CreateNotificationAsync(notification);
                if (!created.Success)
                {
                    await _logService.CreateLogAsync(
                        LogActionsAttribute.SupportTicketUpdated,
                        LogTypeAttribute.Error,
                        "Ticket status notification was not saved. Ticket " + @event.Ticket.TicketID + ". " +
                        string.Join(" ", created.Errors ?? new List<string>()));
                }

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
                    if (employeeNote != null && employeeNote.IsRead)
                    {
                        employeeNote.IsRead = false;
                        await _notificationService.UpdateNotificationAsync(employeeNote);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync(
                    LogActionsAttribute.SupportTicketUpdated,
                    LogTypeAttribute.Error,
                    "Ticket status notification failed. Ticket " + @event.Ticket.TicketID + ". " + ex.Message);
            }
        }
    }
}
