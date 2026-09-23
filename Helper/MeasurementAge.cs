using System;
using System.Collections.Generic;
using System.Globalization;

namespace PoolControl.Helper;

public static class MeasurementAge
{
    public static string Format(DateTime timestamp, DateTime now, CultureInfo culture)
    {
        if (timestamp > now) timestamp = now;
        var years = now.Year - timestamp.Year;
        if (timestamp.AddYears(years) > now) years--;
        var remainder = now - timestamp.AddYears(years);
        var german = culture.TwoLetterISOLanguageName == "de";
        var parts = new List<string>();
        void Add(int value, string unit)
        {
            if (value > 0) parts.Add(value.ToString(culture) + " " + unit);
        }
        Add(years, german ? "J" : "yr");
        Add(remainder.Days, german ? "T" : "d");
        Add(remainder.Hours, "h");
        Add(remainder.Minutes, "min");
        Add(remainder.Seconds, "s");
        return parts.Count == 0 ? "0 s" : string.Join(" ", parts);
    }
}
