using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class ErrorMessages
    {
        public const string Itemsshouldbepassedaspairs = "Items should be passed as pairs: value, label";
        public const string NoPermissionForThisAction = "There is no permission for this action";
        public const string RequiredFieldsValidationFailed = "Validation Failed : {0}";
    }
}
