using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Events
{
    public class UserRegisteredEvent : IEvent
    {
        public User NewUser { get; }
        public bool SendEmail { get; }
        public bool SendNotification { get; }
        public string RegisteredWithResource { get; }

        public UserRegisteredEvent(User newUser, bool sendEmail = true, bool sendNote = true, string registeredWithResource = "Internal")
        {
            NewUser = newUser;
            SendEmail = sendEmail;
            SendNotification = sendNote;
            RegisteredWithResource = registeredWithResource;
        }
    }

    public class UserRegisteredEventHandler : IEventHandler<UserRegisteredEvent>
    {
        private readonly IConfiguration _config;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly IEmailConfirmationTokenService _tokenService;
        private readonly ILogService _logService;

        public UserRegisteredEventHandler(IConfiguration config, INotificationService notificationService, IEmailSender emailSender, IEmailConfirmationTokenService emailConfirmationTokenService, ILogService logService)
        {
            _config = config;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _tokenService = emailConfirmationTokenService;
            _logService = logService;
        }

        public async Task HandleAsync(UserRegisteredEvent @event)
        {
            if (@event.SendEmail == true)
            {
                var emailToken = new EmailConfirmationToken { UserId = @event.NewUser.Id, TokenType = EmailTokenTypeAttribute.Registration };

                var tokenResult = await _tokenService.CreateConfirmationTokenAsync(emailToken);
                if (!tokenResult.Success)
                {
                    foreach (var error in tokenResult.Errors)
                    {
                        await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Error, error);
                    }
                }
                else
                {
                    if (tokenResult?.Data?.UserId != null && tokenResult.Data.Token != null)
                    {
                        var appUrl = _config["AppSettings:BaseUrl"];
                        string templateName = null;
                        string confirmationLink = null;

                        if (@event.RegisteredWithResource == "Internal")
                        {
                            templateName = EmailTemplateNameAttribute.EmailConfirmation;
                            //confirmationLink = $"{appUrl}/Account/ConfirmEmail?userId={tokenResult.Data.UserId}&token={tokenResult.Data.Token}";
                            confirmationLink =
                            $"{appUrl}/Account/ConfirmEmail" +
                            $"?userId={tokenResult.Data.UserId}" +
                            $"&token={Uri.EscapeDataString(tokenResult.Data.Token)}";
                        }
                        else if (@event.RegisteredWithResource == "Google")
                        { 
                            
                        }

                        if (!string.IsNullOrEmpty(templateName) && !string.IsNullOrEmpty(confirmationLink))
                        {
                            var emailModel = new
                            {
                                UserName = $"{@event.NewUser.FirstName} {@event.NewUser.LastName}",
                                ConfirmationLink = confirmationLink,
                            };

                            await _emailSender.SendEmailWithTemplateAsync(templateName, @event.NewUser.Email, emailModel);
                        }   
                    }
                }
            }

            if (@event.SendNotification == true)
            {
                var message = string.Format(Messages.UserRegistrationNotification, @event.NewUser.FirstName);
                Notification notification = new Notification { UserId = @event.NewUser.Id, Type = NotificationTypeAttribute.Information, Message = message, Link = "/Dashboard/MyProfile" };

                await _notificationService.CreateNotificationAsync(notification);
            }

            await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Information, @event.NewUser.Email);
        }
    }
}
