using Microsoft.Extensions.Localization;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class Messages
    {
        private static readonly ResourceManager ResourceManager =
        new ResourceManager(
            "Partnerly.Resources.SharedResource",
            typeof(SharedResource).Assembly);

        public static string Get(string key)
        {
            return ResourceManager.GetString(
                key,
                CultureInfo.CurrentUICulture) ?? key;
        }

        #region Messages
        //public const string RecordSaved = "Запись успешно сохранена!";
        //public const string SupportSuccess = "Ваше сообщение отправлено. Мы свяжемся с вами вскоре.";
        //public const string CaseCreationLimit = "Вы сегодня больше не можете открыть дело.";
        //public const string SiteUnderMaintenanceMode = "Сайт находится в режиме обслуживания.";
        public static string RecordSaved => Get(nameof(RecordSaved));
        public static string SupportSuccess => Get(nameof(SupportSuccess));
        public static string CaseCreationLimit => Get(nameof(CaseCreationLimit));
        public static string SiteUnderMaintenanceMode => Get(nameof(SiteUnderMaintenanceMode));
        #endregion

        #region Notifications
        //public const string UserRegistrationNotification =  "Добро пожаловать, {0}! 🎉 " +
        //                                     "Мы рады видеть вас на нашем сайте.";
        //public const string NewTickedToEmployeeNotification = "У вас новый тикет в службу поддержки. {0}";
        //public const string TickedStartedNotification = "Ваш запрос мы начали смотреть. {0}";
        //public const string TickedReopenedNotification = "Ваш запрос был снова открыт. {0}";
        //public const string TickedClosedNotification = "Ваш запрос был закрыт. {0}";

        public static string UserRegistrationNotification => Get(nameof(UserRegistrationNotification));
        public static string NewTickedToEmployeeNotification => Get(nameof(NewTickedToEmployeeNotification));
        public static string TickedStartedNotification => Get(nameof(TickedStartedNotification));
        public static string TickedReopenedNotification => Get(nameof(TickedReopenedNotification));
        public static string TickedClosedNotification => Get(nameof(TickedClosedNotification));
        #endregion
    }
}
