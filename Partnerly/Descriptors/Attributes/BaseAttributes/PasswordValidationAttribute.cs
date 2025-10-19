using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Partnerly.Descriptors.Attributes.BaseAttributes
{
    public class PasswordValidationAttribute : CusromStringLenghtAttribute
    {
        public bool RequireDigit { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireSpecial { get; set; } = true;

        public PasswordValidationAttribute(int min = 6, int max = 15) : base(min, max) { }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var baseResult = base.IsValid(value, validationContext);

            if (baseResult != ValidationResult.Success)
                return baseResult;

            if (value == null)
                return new ValidationResult(ErrorMessages.PasswordRequired);

            var password = value.ToString();

            if (!string.IsNullOrEmpty(password))
            {
                if (RequireDigit && !Regex.IsMatch(password, @"\d"))
                    return new ValidationResult(ErrorMessages.PasswordRequireDigit);

                if (RequireLowercase && !Regex.IsMatch(password, "[a-z]"))
                    return new ValidationResult(ErrorMessages.PasswordRequireLowercase);

                if (RequireUppercase && !Regex.IsMatch(password, "[A-Z]"))
                    return new ValidationResult(ErrorMessages.PasswordRequireUppercase);

                if (RequireSpecial && !Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]"))
                    return new ValidationResult(ErrorMessages.PasswordRequireSpecial);
            }

            return ValidationResult.Success;
        }
    }
}
