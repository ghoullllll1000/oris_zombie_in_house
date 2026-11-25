using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ORM
{
    public static class OrmEntityValidator
    {
        public static void Validate(object entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var context = new ValidationContext(entity);

            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(
                entity,
                context,
                results,
                validateAllProperties: true);

            if (!isValid)
            {
                string errors = string.Join(
                    Environment.NewLine,
                    results.Select(r => r.ErrorMessage));

                throw new ValidationException(
                    $"Ошибка валидации модели {entity.GetType().Name}:{Environment.NewLine}{errors}");
            }
        }
    }
}
