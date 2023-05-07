using System;
using System.Globalization;
using System.Linq.Expressions;

namespace PoolControl.Helper;

public static class PoolControlHelper
{
    public static string GetPropertyName<T>(Expression<Func<T>> propertyLambda)
    {
        MemberExpression? me = propertyLambda.Body as MemberExpression;
        if (me == null)
        {
            throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
        }

        string result = string.Empty;
        do
        {
            result = me.Member.Name + "." + result;
            me = me.Expression as MemberExpression;
        } while (me != null);

        result = result.Remove(result.Length - 1); // remove the trailing "."
        return result;
    }

    public static decimal getDecimal(string value)
    {
        return decimal.Parse(value, new CultureInfo("en-US"));
    }

    public static string format0Decimal(double value)
    {
        return Math.Round(value, 0).ToString("#0", new CultureInfo("en-US"));
    }

    public static string format1Decimal(double value)
    {
        return Math.Round(value, 1).ToString("#0.0", new CultureInfo("en-US"));
    }

    public static string format2Decimal(double value)
    {
        return Math.Round(value, 2).ToString("#0.00", new CultureInfo("en-US"));
    }

    public static string format3Decimal(double value)
    {
        return Math.Round(value, 3).ToString("#0.000", new CultureInfo("en-US"));
    }
}