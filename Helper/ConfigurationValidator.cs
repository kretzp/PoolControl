using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PoolControl.Helper;

public static class ConfigurationValidator
{
    public static IReadOnlyList<string> Validate(IEnumerable<object?> configuredObjects)
    {
        var errors = new List<string>();
        foreach (var configuredObject in configuredObjects.Where(value => value != null).Distinct())
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(configuredObject!);
            if (Validator.TryValidateObject(configuredObject!, context, validationResults, validateAllProperties: true))
                continue;

            errors.AddRange(validationResults.Select(result =>
                $"{configuredObject!.GetType().Name}: {result.ErrorMessage}"));
        }

        return errors;
    }
}
