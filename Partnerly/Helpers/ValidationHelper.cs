using System.ComponentModel.DataAnnotations;

namespace Partnerly.Helpers
{
    public class ValidationHelper
    {
        public static string? ValidateEntityRequiredFields<T>(T entity, out bool isValid)
        {
            isValid = false;
            string? errors = null;
            if (entity == null) return errors;

            var context = new ValidationContext(entity, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            isValid = Validator.TryValidateObject(entity, context, results, validateAllProperties: true);

            if (!isValid)
            {
                errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            }

            return errors;
        }
    }
}
