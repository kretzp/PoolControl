using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace PoolControl.Helper;

public class Result
{
    public bool Success { get; init; }
    public string? Message { get; init; }
}

public static class PropertySetter
{
    private static bool TrySetProperty(object? parent, PropertyInfo? property, string value, out string? error)
    {
        error = null;
        if (parent == null || property?.CanWrite != true)
        {
            error = "Property does not exist or is read-only.";
            return false;
        }

        object? converted;
        if (property.PropertyType == typeof(decimal))
        {
            converted = decimal.Parse(value, CultureInfo.InvariantCulture);
        }
        else if (property.PropertyType == typeof(int))
        {
            converted = int.Parse(value, CultureInfo.InvariantCulture);
        }
        else if (property.PropertyType == typeof(double))
        {
            converted = double.Parse(value, CultureInfo.InvariantCulture);
            if (!double.IsFinite((double)converted))
            {
                error = "Non-finite numeric values are not allowed.";
                return false;
            }
        }
        else if (property.PropertyType == typeof(float))
        {
            converted = float.Parse(value, CultureInfo.InvariantCulture);
            if (!float.IsFinite((float)converted))
            {
                error = "Non-finite numeric values are not allowed.";
                return false;
            }
        }
        else if (property.PropertyType == typeof(string))
        {
            converted = value;
        }
        else if (property.PropertyType == typeof(bool))
        {
            converted = value.Trim().ToLowerInvariant() switch
            {
                "1" or "true" or "on" or "yes" => true,
                "0" or "false" or "off" or "no" => false,
                _ => null
            };
            if (converted == null)
            {
                error = "Boolean values must be true/false, 1/0, on/off or yes/no.";
                return false;
            }
        }
        else if (property.PropertyType == typeof(TimeSpan))
        {
            converted = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }
        else
        {
            error = $"Type {property.PropertyType.Name} is not supported.";
            return false;
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(parent) { MemberName = property.Name };
        if (!Validator.TryValidateProperty(converted, validationContext, validationResults))
        {
            error = string.Join(" ", validationResults.Select(result => result.ErrorMessage));
            return false;
        }

        property.SetValue(parent, converted);
        return true;
    }

    /*
     * baseObject
     *   |
     *   --- objectNameToSet[key]
     *            |
     *            ---propertyName = propertyValue
     * or
     * 
     * baseObject
     *   |
     *   ---propertyName = propertyValue
     *            
     */
    public static Result setProperty(object? baseObject, string propertyName, string propertyValue, string objectNameToSet = "", string key = "")
    {
        var info = $"setting Object: {baseObject} Object2: {objectNameToSet} Key: {key} Property: {propertyName} Value: {propertyValue}";
            
        try
        {
            object? propertyObject;
            if (string.IsNullOrEmpty(objectNameToSet))
            {
                propertyObject = baseObject;
            }
            else
            {
                if (string.IsNullOrEmpty(key))
                {
                    propertyObject = baseObject?.GetType().GetProperty(objectNameToSet)?.GetValue(baseObject);
                }
                else
                {
                    var dict = (Dictionary<string, object>)baseObject?.GetType().GetProperty(objectNameToSet + "Obj")?.GetValue(baseObject)!;

                    propertyObject = dict[key];
                }
            }

            var childPropertyInfo = propertyObject?.GetType().GetProperty(propertyName);

            if (!TrySetProperty(propertyObject, childPropertyInfo, propertyValue, out var error))
            {
                return new Result { Success = false, Message = $"{error} Error while setting Object: {propertyObject} Object2: {objectNameToSet} Key: {key} Property: {childPropertyInfo?.Name} Value: {propertyValue}" };
            }
        }
        catch (Exception ex)
        {
            return new Result { Success = false, Message = $"{ex.Message} while {info}" };
        }

        return new Result { Success = true, Message = "Success: {info}" };
    }
}
