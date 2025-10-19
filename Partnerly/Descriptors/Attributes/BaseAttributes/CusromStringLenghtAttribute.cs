using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Descriptors.Attributes.BaseAttributes
{
    public class CusromStringLenghtAttribute : StringLengthAttribute
    {
        public int Min { get; }
        public int Max { get; }

        public CusromStringLenghtAttribute(int min = 0, int max = int.MaxValue) : base(max)
        {
            Min = min;
            Max = max;
            ErrorMessage = min > 0 ? string.Format(ErrorMessages.StringMinMaxLength, min, max) : ErrorMessage = string.Format(ErrorMessages.StringMaxLength, max);
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; 

            var str = value.ToString();
            if (str?.Length >= Min && str.Length <= Max)
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage);
        }
    }
}
