using System;
using System.ComponentModel.DataAnnotations;

namespace PoolControl.ViewModels;

public interface IUsesIntervalTimer
{
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class TimerIntervalAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not int seconds || seconds < 0 || seconds > 86400)
            return new ValidationResult("IntervalInSec must be between 0 and 86400 seconds.");

        if (validationContext.ObjectInstance is IUsesIntervalTimer && seconds == 0)
            return new ValidationResult("IntervalInSec must be between 1 and 86400 seconds.");

        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DailyTimeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        return value is TimeSpan time && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1)
            ? ValidationResult.Success
            : new ValidationResult($"{validationContext.MemberName} must be a time between 00:00:00 and 23:59:59.");
    }
}
