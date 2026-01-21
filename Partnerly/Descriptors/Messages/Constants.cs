using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class Constants
    {
        public const string SuperReferralCode = "BRANCH01";
        public const string SuperUserEmail = "marat.iigservices@gmail.com";
        public const string SuperUserPassword = "MarDan123!";
        public const string SuperUserFirstName = "Marat";
        public const string SuperUserLastName = "Danielyan";
        public const string SuperUserPhone = "+37497111312";
        public const string DefaultUserProfilePhotoPath = "~/assets/img/profiles/default-user.png";

        // Roles
        public const string View = "View";
        public const string Update = "Update";
        public const string Delete = "Delete";
        //
        public const string DeleteConfirmed = "DeleteConfirmed";
        public const string Create = "Create";

        // Payments
        public const string Open = "Открыто";
        public const string Pending = "В ожидании";
        public const string Payed = "Оплачено"; 
        public const string Closed = "Закрыто";
        public const string Canceled = "Отменено";
        public const string Rejected = "Отклоненный";

        // Log 
        public const string UserSignIn = "Вход пользователя";
        public const string UserSignOut = "Выход пользователя";

        public const string UserCreated = "Создание пользователя";
        public const string UserDeleted = "Удаление пользователя";
        public const string UserUpdated = "Обновление пользователя";

        public const string RoleCreated = "Создание роли";
        public const string RoleDeleted = "Удаление роли";
        public const string RoleUpdated = "Обновление роли";

        public const string PaymentCreated = "Создание платежа";
        public const string PaymentDeleted = "Удаление платежа";
        public const string PaymentUpdated = "Обновление платежа";

        public const string SystemSettingsCreated = "System Settings Creating";
        public const string SystemSettingsDeleted = "System Settings Deleting";
        public const string SystemSettingsUpdated = "System Settings Updating";

        public const string EmailConfirmationTokenCreated = "Создание токена подтверждения электронной почты";
        public const string EmailConfirmationTokenDeleted = "Удаление токена подтверждения электронной почты";
        public const string EmailConfirmationTokenUpdated = "Обновление токена подтверждения электронной почты";

        public const string EmailTemplateCreated = "Создание шаблона электронной почты";
        public const string EmailTemplateDeleted = "Удаление шаблона электронной почты";
        public const string EmailTemplateUpdated = "Обновление шаблона электронной почты";

        public const string NotificationCreated = "Создание уведомлений";
        public const string NotificationDeleted = "Удаление уведомлений";
        public const string NotificationUpdated = "Обновление уведомлений";

        public const string UserGroupCreated = "Создание группы пользователей";
        public const string UserGroupDeleted = "Удаление группы пользователей";
        public const string UserGroupUpdated = "Обновление группы пользователей";

        public const string SupportTicketCreated = "Создание тикета поддержки";
        public const string SupportTicketDeleted = "Удаление тикета поддержки";
        public const string SupportTicketUpdated = "Обновление тикета поддержки";

        public const string EmailSending = "Отправка электронной почты"; 

        // Log types
        public const string Information = "Информация";
        public const string Warning = "Предупреждение";
        public const string Error = "Ошибка";
        public const string Critical = "Критический";

        // Notofocation
        public const string Success = "Успешно";

        // Email Template Names
        public const string EmailConfirmation = "Email Confirmation";

        // Email Token Types
        public const string Registration = "Registration";
        public const string ForgotPassword = "Forgot Password";
        public const string CriticalError = "Critical Error";

        // ClaimTypes
        public const string ClaimTypeRoleType = "RoleType";
        public const string ClaimTypeFirstName = "FirstName";
        public const string ClaimTypeLastName = "LastName";
        public const string ClaimTypeReffCode = "ReffCode";
        public const string ClaimTypeUserPhotoUrl = "UserPhotoUrl";
        public const string ClaimTypeLoginTime = "LoginTime";

        // Others
        public const string Edit = "Edit";

        // Support Ticket statuses
        public const string New = "Новый";
        public const string InProgress = "В ходе выполнения";
    }
}
