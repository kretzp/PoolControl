using System;
using System.Globalization;
using System.Reflection;

namespace PoolControl.Helper;

/// <summary>Optimistic edit: a touch draft cannot silently overwrite an MQTT update.</summary>
public sealed class UiEditSession
{
    private readonly object _target;
    private readonly PropertyInfo _property;
    public object? Original { get; }

    public UiEditSession(object target, string property)
    {
        _target = target;
        _property = target.GetType().GetProperty(property) ?? throw new ArgumentException(property);
        Original = _property.GetValue(target);
    }

    public bool HasConflict => !Equals(Original, _property.GetValue(_target));

    public bool TryNumber(string text, CultureInfo culture, decimal minimum, decimal maximum, out string error)
    {
        error = "value";
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                culture, out var value) || value < minimum || value > maximum) return false;
        if (_property.PropertyType == typeof(int) && decimal.Truncate(value) != value) return false;
        return TryApply(value.ToString(CultureInfo.InvariantCulture), out error);
    }

    public bool TryTime(int hour, int minute, out string error)
    {
        error = "value";
        return hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59
            && TryApply(new TimeSpan(hour, minute, 0).ToString("c", CultureInfo.InvariantCulture), out error);
    }

    public bool TryApply(string invariantValue, out string error)
    {
        error = "conflict";
        if (HasConflict) return false;
        var result = PropertySetter.setProperty(_target, _property.Name, invariantValue);
        error = result.Success ? "" : "value";
        return result.Success;
    }
}
