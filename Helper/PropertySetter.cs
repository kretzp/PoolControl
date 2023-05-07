using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace PoolControl.Helper;

public class Result
{
    public bool Success { get; init; }
    public string? Message { get; init; }
}

public static class PropertySetter
{
    private static bool setProperty(object? parent, PropertyInfo? myPropInfo, string value)
    {
        if (myPropInfo?.PropertyType == typeof(decimal))
        {
            myPropInfo.SetValue(parent, decimal.Parse(value, new CultureInfo("en-US")));
        }
        else if (myPropInfo?.PropertyType == typeof(int))
        {
            myPropInfo.SetValue(parent, int.Parse(value, new CultureInfo("en-US")));
        }
        else if (myPropInfo?.PropertyType == typeof(double))
        {
            myPropInfo.SetValue(parent, double.Parse(value, new CultureInfo("en-US")));
        }
        else if (myPropInfo?.PropertyType == typeof(float))
        {
            myPropInfo.SetValue(parent, float.Parse(value, new CultureInfo("en-US")));
        }
        else if (myPropInfo?.PropertyType == typeof(string))
        {
            myPropInfo.SetValue(parent, value);
        }
        else if (myPropInfo?.PropertyType == typeof(bool))
        {
            myPropInfo.SetValue(parent, !(value.StartsWith("0") || value.ToLower().StartsWith("fa") || value.ToLower().StartsWith("of")));
        }
        else
        {
            return false;
        }

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
            
        PropertyInfo? childPropertyInfo = null;

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

                childPropertyInfo = propertyObject?.GetType().GetProperty(propertyName);
            }

            if (!setProperty(propertyObject, childPropertyInfo, propertyValue))
            {
                return new Result { Success = false, Message = $"Error while setting Object: {propertyObject} Object2: {objectNameToSet} Key: {key} Property: {childPropertyInfo?.Name} Value: {propertyValue}" };
            }
        }
        catch (Exception ex)
        {
            return new Result { Success = false, Message = $"{ex.Message} while {info}" };
        }

        return new Result { Success = true, Message = "Success: {info}" };
    }
}