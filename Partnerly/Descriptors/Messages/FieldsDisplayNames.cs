using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class FieldsDisplayNames
    {
        //public const string Email = "Электронная почта";
        //public const string Password = "Пароль";
        //public const string NewPassword = "Новый пароль";
        //public const string ConfirmPassword = "Подтверждение пароля";
        //public const string RememberMe = "Запомнить меня";
        //public const string Phone = "Телефон";
        //public const string FirstName = "Имя";
        //public const string LastName  = "Фамилия";
        //public const string ReferrerCode = "Реферальный код";
        //public const string ReffererName = "Имя реферера";
        //public const string TermsConditions = "политика конфиденциальности & условия";
        //public const string Subject = "Заголовок";
        //public const string BodyHTML = "Тело HTML";
        //public const string Balance = "Баланс";
        //public const string LastActivity = "Последняя активность";
        //public const string IsOnlayn = "Онлайн";
        //public const string IsBlocked = "Заблокировано";
        //public const string EmailConfirmed = "Почта подтверждена";
        //public const string Role = "Роль";
        //public const string NewRole = "Новая роль";
        //public const string TemplateType = "Тип шаблона";
        //public const string AllowSendNotifications = "Уведомления";
        //public const string AllowSendEmails = "Эмейл рассылки";

        //public const string Edit = "Редактировать";
        //public const string Delete = "Удалить";
        //public const string Details = "Подробности";

        //public const string CreatedDate = "Дата создания";
        //public const string CreatorName = "Создано";
        //public const string UpdatedDate = "Дата обновления";
        //public const string UpdaterName = "Обновлено";

        //public const string MaintenanceMode = "Режим обслуживания";
        //public const string EmailConfirmationTokenHours = "токен Подтвержд. Email (часы)";
        //public const string ForgotPasswordTokenExpiredAtHours = "токен Забыли пароль (часы)";
        //public const string Automaticonlinestatusupdate = "онлайн-статус (минута)";
        //public const string ScrollingText = "Бегущая строка";

        //public const string Save = "Сохранить";
        //public const string Create = "Создать";
        //public const string More = "Дополнительно";
        //public const string Back = "Назад";
        //public const string Action = "Действие";
        //public const string Type = "Тип";
        //public const string Message = "Сообщение";
        //public const string FilePath = "Путь к файлу";
        //public const string Method = "Метод";
        //public const string LineNumber = "Номер строки";

        //public const string User = "Пользователь";

        //public const string Name = "Названия";
        //public const string Description = "Описание";
        //public const string Group = "Группа";
        //public const string IsRead = "Прочитано";
        //public const string Status = "Статус";
        //public const string AssignedTo = "Назначено";
        //public const string TaskNumber = "Номер задачи";
        //public const string Viewcase = "Посмотреть дело";


        public static string Email => Messages.Get(nameof(Email));
        public static string Password => Messages.Get(nameof(Password));
        public static string NewPassword => Messages.Get(nameof(NewPassword));
        public static string ConfirmPassword => Messages.Get(nameof(ConfirmPassword));
        public static string RememberMe => Messages.Get(nameof(RememberMe));
        public static string Phone => Messages.Get(nameof(Phone));
        public static string FirstName => Messages.Get(nameof(FirstName));
        public static string LastName => Messages.Get(nameof(LastName));
        public static string FullName => Messages.Get(nameof(FullName));
        public static string ReferrerCode => Messages.Get(nameof(ReferrerCode));
        public static string ReffererName => Messages.Get(nameof(ReffererName));
        public static string TermsConditions => Messages.Get(nameof(TermsConditions));
        public static string Subject => Messages.Get(nameof(Subject));
        public static string BodyHTML => Messages.Get(nameof(BodyHTML));
        public static string Balance => Messages.Get(nameof(Balance));
        public static string LastActivity => Messages.Get(nameof(LastActivity));
        public static string IsOnlayn => Messages.Get(nameof(IsOnlayn));
        public static string IsBlocked => Messages.Get(nameof(IsBlocked));
        public static string EmailConfirmed => Messages.Get(nameof(EmailConfirmed));
        public static string Role => Messages.Get(nameof(Role));
        public static string NewRole => Messages.Get(nameof(NewRole));
        public static string TemplateType => Messages.Get(nameof(TemplateType));
        public static string AllowSendNotifications => Messages.Get(nameof(AllowSendNotifications));
        public static string AllowSendEmails => Messages.Get(nameof(AllowSendEmails));

        public static string Edit => Messages.Get(nameof(Edit));
        public static string Delete => Messages.Get(nameof(Delete));
        public static string Details => Messages.Get(nameof(Details));

        public static string CreatedDate => Messages.Get(nameof(CreatedDate));
        public static string CreatorName => Messages.Get(nameof(CreatorName));
        public static string UpdatedDate => Messages.Get(nameof(UpdatedDate));
        public static string UpdaterName => Messages.Get(nameof(UpdaterName));

        public static string MaintenanceMode => Messages.Get(nameof(MaintenanceMode));
        public static string EmailConfirmationTokenHours => Messages.Get(nameof(EmailConfirmationTokenHours));
        public static string ForgotPasswordTokenExpiredAtHours => Messages.Get(nameof(ForgotPasswordTokenExpiredAtHours));
        public static string Automaticonlinestatusupdate => Messages.Get(nameof(Automaticonlinestatusupdate));
        public static string ScrollingText => Messages.Get(nameof(ScrollingText));

        public static string Save => Messages.Get(nameof(Save));
        public static string Create => Messages.Get(nameof(Create));
        public static string More => Messages.Get(nameof(More));
        public static string Back => Messages.Get(nameof(Back));
        public static string Action => Messages.Get(nameof(Action));
        public static string Type => Messages.Get(nameof(Type));
        public static string Message => Messages.Get(nameof(Message));
        public static string FilePath => Messages.Get(nameof(FilePath));
        public static string Method => Messages.Get(nameof(Method));
        public static string LineNumber => Messages.Get(nameof(LineNumber));

        public static string User => Messages.Get(nameof(User));

        public static string Name => Messages.Get(nameof(Name));
        public static string Description => Messages.Get(nameof(Description));
        public static string Group => Messages.Get(nameof(Group));
        public static string IsRead => Messages.Get(nameof(IsRead));
        public static string Status => Messages.Get(nameof(Status));
        public static string AssignedTo => Messages.Get(nameof(AssignedTo));
        public static string TaskNumber => Messages.Get(nameof(TaskNumber));
        public static string Viewcase => Messages.Get(nameof(Viewcase));

        public static string Closed => Messages.Get(nameof(Closed));
        public static string InProgress => Messages.Get(nameof(InProgress));
        public static string New => Messages.Get(nameof(New));

    }
}
