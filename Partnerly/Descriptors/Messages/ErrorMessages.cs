using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class ErrorMessages
    {
        #region System Errors
        public const string Itemsshouldbepassedaspairs = "Items should be passed as pairs: value, label";
        public const string NoPermissionForThisAction = "There is no permission for this action";
        public const string RequiredFieldsValidationFailed = "Validation Failed : {0}";
        #endregion

        #region Views errors
        public const string ReferrerUserCannotBeFoundOrInactive = "Реферер-пользователь не найден или неактивен";
        public const string IncorectPasswordOrUsername = "Неверный логин или пароль";
        public const string UserAccessDenied = "у пользователя нету доступа";
        public const string FieldRequired = "oбязательное поле";
        public const string EmailRequired = $"{FieldsDisplayNames.Email} {FieldRequired}";
        public const string PasswordRequired = $"{FieldsDisplayNames.Password} {FieldRequired}";
        public const string CompareConfirmPassword = "Пароли не совпадают.";
        public const string TypeValidPhoneNumber = "Введите правильный номер в формате +374 XX XXXXXX";
        public const string InvalidRefferalCode = "Такого реферального кода не существует";
        #endregion
    }
}
