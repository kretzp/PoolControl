using Newtonsoft.Json;
using PoolControl.Time;
using System;

namespace PoolControl.ViewModels;

/// <summary>
/// Base Data for pumps
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public abstract class PumpModel : ViewModelBase
{
    protected TimeSpan Daily = new TimeSpan(24, 0, 0);

    [JsonIgnore]
    public Switch? Switch { get; set; }

    public abstract void OnTemperatureChange(MeasurementArgs args);

    public abstract void RecalculateThings();

    public void StartTimerTriggered()
    {
        Logger.Debug("Switching {On} on", GetType().Name);

        if (WinterMode)
        {
            Logger.Information("WinterMode! Nothing to do!");
            return;
        }

        if (Switch != null)
        {
            Switch.On = true;
        }
        else
        {
            Logger.Debug("{Name} is null and could not be turned on", GetType().Name);
        }
        RecalculateThings();
    }

    public void EndTimerTriggered()
    {
        Logger.Debug("Switching {Name} off", GetType().Name);

        if (WinterMode)
        {
            Logger.Information("WinterMode! Nothing to do!");
        }

        if (Switch != null)
        {
            Switch.On = false;
        }
        else
        {
            Logger.Debug("{Name} is null and could not be turned off", GetType().Name);
        }
        RecalculateThings();
    }

    protected TimeTrigger InitializeTrigger(Action? action, TimeSpan period, string name)
    {
        var trigger = new TimeTrigger
        {
            Name = name,
            Period = period
        };
        trigger.OnTimeTriggered += action;

        return trigger;
    }

    protected void startTrigger(TimeTrigger trigger, TimeSpan startTime)
    {
        trigger.StartTime = startTime;
        trigger.InitiateTimer();
    }

    protected string GetTimerName(string name)
    {
        return $"{GetType().Name}.{name}";
    }
}