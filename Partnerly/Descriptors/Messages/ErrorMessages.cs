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
        public const string DefaultLogMessage = "Unknown Information";
        public const string DefaultLogErrorMessage = "Unknown validation error";
        public const string RecordIsNullFromController = "{0} is null from controller";
        public const string Cannotbefound = "{0} cannot be found";
        public const string JsonDeserializeProblem = "Unable to deserialize JSON object into email attachment {0}, error message - {1}";
        #endregion

        #region Views errors
        public static string ConfirmDelete => Messages.Get(nameof(ConfirmDelete));
        public static string Deletingthenotification => Messages.Get(nameof(Deletingthenotification));
        public static string Deletingthetemplate => Messages.Get(nameof(Deletingthetemplate));
        public static string Deletetask => Messages.Get(nameof(Deletetask));
        public static string Deletegroup => Messages.Get(nameof(Deletegroup));
        public static string DeleteUser => Messages.Get(nameof(DeleteUser)); 
        public static string DeleteRole => Messages.Get(nameof(DeleteRole)); 
        public static string ReferrerUserCannotBeFoundOrInactive => Messages.Get(nameof(ReferrerUserCannotBeFoundOrInactive));
        public static string InvalidEmailOrPassword => Messages.Get(nameof(InvalidEmailOrPassword));
        public static string IncorrectPassword => Messages.Get(nameof(IncorrectPassword));
        public static string InvalidEmail => Messages.Get(nameof(InvalidEmail));
        public static string UserAccessDenied => Messages.Get(nameof(UserAccessDenied));
        public static string FieldRequired => Messages.Get(nameof(FieldRequired));
        public static string UniqueValue => Messages.Get(nameof(UniqueValue));
        public static string CannotDeleteSystemRecords => Messages.Get(nameof(CannotDeleteSystemRecords));
        public static string NotFound => Messages.Get(nameof(NotFound));
        public static string TypeValidPhoneNumber => Messages.Get(nameof(TypeValidPhoneNumber));
        public static string InvalidReferralCode => Messages.Get(nameof(InvalidReferralCode));
        public static string UserWithEmailAlreadyExists => Messages.Get(nameof(UserWithEmailAlreadyExists));
        public static string UserWithPhoneAlreadyExists => Messages.Get(nameof(UserWithPhoneAlreadyExists));
        public static string ReadPolicyAndTerms => Messages.Get(nameof(ReadPolicyAndTerms));
        public static string StringMaxLength => Messages.Get(nameof(StringMaxLength));
        public static string StringMinMaxLength => Messages.Get(nameof(StringMinMaxLength));
        public static string EmailRequired => Messages.Get(nameof(EmailRequired));
        public static string PasswordRequired => Messages.Get(nameof(PasswordRequired));
        public static string CompareConfirmPassword => Messages.Get(nameof(CompareConfirmPassword));
        public static string PasswordRequireDigit => Messages.Get(nameof(PasswordRequireDigit));
        public static string PasswordRequireLowercase => Messages.Get(nameof(PasswordRequireLowercase));
        public static string PasswordRequireUppercase => Messages.Get(nameof(PasswordRequireUppercase));
        public static string PasswordRequireSpecial => Messages.Get(nameof(PasswordRequireSpecial));
        public static string LoginEmailConfirmationMessage => Messages.Get(nameof(LoginEmailConfirmationMessage));
        public static string UserIsBlocked => Messages.Get(nameof(UserIsBlocked));
        public static string LinkIsExpired => Messages.Get(nameof(LinkIsExpired));
        public static string LinkExpiredDetail => Messages.Get(nameof(LinkExpiredDetail));

        public static string PhotoWasNotSelected => Messages.Get(nameof(PhotoWasNotSelected));
        public static string InvalidImageFormat => Messages.Get(nameof(InvalidImageFormat));
        public static string Maximumfilesizeis => Messages.Get(nameof(Maximumfilesizeis));
        public static string Photosavedsuccessfully => Messages.Get(nameof(Photosavedsuccessfully));
        #endregion
    }
}
