using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SongsWithChords.Extensions
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class OneRequiredPropAttribute : ValidationAttribute
    {
        private readonly string[] _propertyNames;

        public OneRequiredPropAttribute(params string[] propertyNames)
        {
            _propertyNames = propertyNames;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return new ValidationResult("The object cannot be null.");

            var type = value.GetType();
            var nonNullCount = 0;

            foreach (var propertyName in _propertyNames)
            {
                var property = type.GetProperty(propertyName);
                if (property == null)
                {
                    return new ValidationResult($"Property '{propertyName}' not found on type '{type.Name}'.");
                }

                var propertyValue = property.GetValue(value);
                if (propertyValue != null)
                {
                    nonNullCount++;
                }
            }

            if (nonNullCount == 1)
            {
                return ValidationResult.Success; // Exactly one property is non-null
            }

            if (nonNullCount == 0)
            {
                return new ValidationResult($"At least one of the following properties must be provided: {string.Join(", ", _propertyNames)}.");
            }

            return new ValidationResult($"Only one of the following properties can be provided: {string.Join(", ", _propertyNames)}.");
        }
    }




}
