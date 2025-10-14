using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class Messages
    {
        #region Messages
        public const string RecordSaved = "Запись успешно сохранена!";
        #endregion

        #region Notifications
        public const string UserRegistrationNotification =  "Добро пожаловать, {0}! 🎉 " +
                                                            "Мы рады видеть вас на нашем сайте.";
        #endregion
    }
}
