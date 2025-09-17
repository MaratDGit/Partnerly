using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Partnerly.Descriptors.Attributes.BaseAttributes
{
    public class ArmenianPhoneAttribute : ValidationAttribute
    {
        private static readonly Regex _regex =
        new Regex(@"^\+374(77|91|93|94|98|49|55|95|41|44|99|96|43|97)\d{6}$");

        public override bool IsValid(object value)
        {
            if (value == null) return false;
            return _regex.IsMatch(value.ToString().Replace(" ", ""));
        }
    }
}
