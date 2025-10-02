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
        public const string DefaultLogErrorMessage = "Unknown validation error";
        public const string RecordIsNullFromController = "{0} is null from controller";
        public const string Cannotbefound = "{0} cannot be found";
        public const string JsonDeserializeProblem = "Unable to deserialize JSON object into email attachment {0}, error message - {1}";
        #endregion

        #region Views errors
        public const string ReferrerUserCannotBeFoundOrInactive = "Реферер-пользователь не найден или неактивен";
        public const string IncorectPasswordOrUsername = "Неверный логин или пароль";
        public const string IncorectEmail = "Неверный Email";
        public const string UserAccessDenied = "у пользователя нету доступа";
        public const string FieldRequired = "oбязательное поле";
        public const string TypeValidPhoneNumber = "Введите правильный номер в формате +374 XX XXXXXX";
        public const string InvalidRefferalCode = "Такого реферального кода не существует";
        public const string UserWithEmailArleadyExist = "Пользователь с таким Email уже существует";
        public const string UserWithPhoneArleadyExist = "Пользователь с таким телефоном уже существует";
        public const string ReadPolicyAndTerms = "Необходимо согласиться с условиями";
        public const string StringMaxLength = "Длина строки должна быть до {0} символов";
        public const string StringMinMaxLength = "Длина строки должна быть от {0} до {1} символов";
        public const string EmailRequired = $"{FieldsDisplayNames.Email} {FieldRequired}";
        public const string PasswordRequired = $"{FieldsDisplayNames.Password} {FieldRequired}";
        public const string CompareConfirmPassword = "Пароли не совпадают";
        public const string PasswordRequireDigit = "Пароль должен содержать хотя бы одну цифру";
        public const string PasswordRequireLowercase = "Пароль должен содержать хотя бы одну строчную букву";
        public const string PasswordRequireUppercase = "Пароль должен содержать хотя бы одну заглавную букву";
        public const string PasswordRequireSpecial = "Пароль должен содержать хотя бы один спецсимвол";
        public const string LoginEmailConfirmationMessage = "Чтобы войти в систему, пожалуйста, подтвердите адрес вашей электронной почты. Мы отправили письмо с инструкциями на указанный вами адрес. Проверьте почту и перейдите по ссылке подтверждения.";
        public const string UserIsBlocked = "Пользователь заблокирован";
        public const string LinkIsExpired = "Ссылка больше недействительна";
        public const string LinkExpiredDetail = "Похоже, что срок действия этой ссылки закончился. Чтобы завершить подтверждение email, получите новую ссылку.";
        #endregion
    }
}
