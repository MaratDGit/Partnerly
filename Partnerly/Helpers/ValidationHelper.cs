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

            Validator.TryValidateObject(entity, context, results, validateAllProperties: true);

            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                var isRequired = Attribute.IsDefined(prop, typeof(RequiredAttribute));
                if (!isRequired) continue;

                var value = prop.GetValue(entity);

                if (value is string str && string.IsNullOrWhiteSpace(str))
                {
                    results.Add(new ValidationResult($"{prop.Name} is required."));
                }

                if (value is Guid guid && guid == Guid.Empty)
                {
                    results.Add(new ValidationResult($"{prop.Name} is required."));
                }

                if (value is Guid?)
                {
                    Guid? nullableGuid = (Guid?)value;

                    if (!nullableGuid.HasValue || nullableGuid.Value == Guid.Empty)
                        results.Add(new ValidationResult($"{prop.Name} is required."));
                }
            }

            if (results.Any())
            {
                isValid = false;
                errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            }
            else
            {
                isValid = true;
            }

            return errors;
        }

    }
}
