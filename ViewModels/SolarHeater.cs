using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Helper;
using PoolControl.Time;

namespace PoolControl.ViewModels;

/// <summary>
/// Data for solar heater
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class SolarHeater : PumpModel
{
    public SolarHeater()
    {
        Logger = Log.Logger?.ForContext<SolarHeater>() ?? throw new ArgumentNullException(nameof(Logger));
        TurnOnTrigger = InitializeTrigger(StartTimerTriggered, Daily, GetTimerName(PoolControlHelper.GetPropertyName(() => TurnOnTrigger)));
        TurnOffTrigger = InitializeTrigger(EndTimerTriggered, Daily, GetTimerName(PoolControlHelper.GetPropertyName(() => TurnOffTrigger)));

        this.WhenAnyValue(x => x.TurnOnDiff).Subscribe(switchOnDiff => PublishMessageWithType(PoolControlHelper.GetPropertyName(() => TurnOnDiff), PoolControlHelper.format1Decimal(switchOnDiff), true));
        this.WhenAnyValue(x => x.TurnOffDiff).Subscribe(switchOffDiff => PublishMessageWithType(PoolControlHelper.GetPropertyName(() => TurnOffDiff), PoolControlHelper.format1Decimal(switchOffDiff), true));
        this.WhenAnyValue(x => x.MaxPoolTemp).Subscribe(maxPoolTemp => PublishMessageWithType(PoolControlHelper.GetPropertyName(() => MaxPoolTemp), PoolControlHelper.format1Decimal(maxPoolTemp), true));
        this.WhenAnyValue(x => x.SolarHeaterCleaningDuration).Subscribe(cleaningDuration => { PublishMessageWithType(PoolControlHelper.GetPropertyName(() => SolarHeaterCleaningDuration), cleaningDuration.ToString(), true); RecalculateSolarHeatingCleaning(); });
        this.WhenAnyValue(x => x.SolarHeaterCleaningTime).Subscribe(cleaningPoint => { PublishMessageWithType(PoolControlHelper.GetPropertyName(() => SolarHeaterCleaningTime), cleaningPoint.ToString(), true); RecalculateSolarHeatingCleaning(); });
    }

    public override void OnTemperatureChange(MeasurementArgs args)
    {
        if (args.BaseMeasurement is { ModelBase: not null })
        {
            var temp = (Temperature)args.BaseMeasurement.ModelBase;
            switch (temp?.Key)
            {
                case "SolarPreRun":
                    SolarPreLoopTemperature = temp;
                    break;
                case "SolarHeater":
                    SolarHeaterTemperature = temp;
                    break;
                case "Pool":
                    SolarPoolTemperature = temp;
                    break;
            }
        }

        if (SolarPreLoopTemperature == null || SolarHeaterTemperature == null || SolarPoolTemperature == null)
        {
            Logger.Debug($"SolarPreLoopTemperature or SolarHeaterTemperature or SolarPoolTemperature is null");
            return;
        }

        Logger.Debug($"SolarPreLoopTemperature: {SolarPreLoopTemperature.Value} SolarHeaterTemperature: {SolarHeaterTemperature.Value} SolarPoolTemperature: {SolarPoolTemperature.Value}");

        DateTime now = DateTime.Now;
        if (now < NextEnd && now > NextEnd - new TimeSpan(0, 0, SolarHeaterCleaningDuration))
        {
            Logger.Information("Do nothing because cleaning of solar heater is running");
            return;
        }

        Logger.Information(SolarPoolTemperature.Value > SolarPreLoopTemperature.Value
            ? "Using SolarPreLoop Temperature vor SolarHeating!"
            : "Using Pool Temperature vor SolarHeating!");

        var baseTemperature = Math.Min(SolarPreLoopTemperature.Value, SolarPoolTemperature.Value);

        if (SolarHeaterTemperature.Value > baseTemperature + TurnOnDiff)
        {
            Switch!.On = baseTemperature < MaxPoolTemp;
            Logger.Information($"{Switch.Name} On({Switch.On}) SolarHeater({SolarHeaterTemperature.Value:#0.0}) > Pool({baseTemperature:#0.0}) + Ein({TurnOnDiff:#0.0}) = Sum({baseTemperature:#0.0}) Max({MaxPoolTemp:#0.0})");
        }
        else if (SolarHeaterTemperature.Value < baseTemperature + TurnOffDiff)
        {
            Switch!.On = false;
                
            Logger.Information($"{Switch.Name} On({Switch.On}) SolarHeater({SolarHeaterTemperature.Value:#0.0}) > Pool({baseTemperature:#0.0}) + Aus({TurnOffDiff:#0.0}) = Sum({baseTemperature:#0.0}) Max({MaxPoolTemp:#0.0})");
        }
    }

    public void RecalculateSolarHeatingCleaning()
    {
        StartTrigger(TurnOnTrigger, SolarHeaterCleaningTime);
        StartTrigger(TurnOffTrigger, SolarHeaterCleaningTime.Add(new TimeSpan(0, 0, SolarHeaterCleaningDuration)));
        NextStart = TurnOnTrigger.TriggerTime;
        NextEnd = TurnOffTrigger.TriggerTime;
    }

    public override void RecalculateThings()
    {
        RecalculateSolarHeatingCleaning();   
    }

    protected override void OnTimerTicked(object? state)
    {
        // Nothing has to be done
    }

    [JsonIgnore]
    protected Temperature? SolarPoolTemperature { get; set; }

    [JsonIgnore]
    protected Temperature? SolarPreLoopTemperature { get; set; }

    [JsonIgnore]
    protected Temperature? SolarHeaterTemperature { get; set; }

    [JsonIgnore]
    public TimeTrigger TurnOnTrigger { get; set; }
        
    [JsonIgnore]
    public TimeTrigger TurnOffTrigger { get; set; }

    [Reactive]
    [JsonProperty]
    public TimeSpan SolarHeaterCleaningTime { get; set; }

    [Reactive]
    [JsonProperty]
    public double TurnOnDiff { get; set; }

    [Reactive]
    [JsonProperty]
    public double TurnOffDiff { get; set; }

    [Reactive]
    [JsonProperty]
    public double MaxPoolTemp   { get; set; }

    [Reactive]
    [JsonProperty]
    public int SolarHeaterCleaningDuration { get; set; }

    [Reactive]
    [JsonProperty]
    public DateTime NextStart { get; set; }

    [Reactive]
    [JsonProperty]
    public DateTime NextEnd { get; set; }
}