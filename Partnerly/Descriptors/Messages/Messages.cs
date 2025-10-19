using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class Messages
    {
        #region Messages
        public const string RecordSaved = "Запись успешно сохранена!";
        public const string SupportSuccess = "Ваше сообщение отправлено. Мы свяжемся с вами вскоре.";
        public const string CaseCreationLimit = "Вы сегодня больше не можете открыть дело.";
        #endregion

        #region Notifications
        public const string UserRegistrationNotification =  "Добро пожаловать, {0}! 🎉 " +
                                                            "Мы рады видеть вас на нашем сайте.";
        public const string NewTickedToEmployeeNotification = "У вас новый тикет в службу поддержки. {0}";
        #endregion
    }
}
