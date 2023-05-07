using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Helper;
using PoolControl.Time;

namespace PoolControl.ViewModels;

/// <summary>
/// This class ist used to hold the data of filter pump, the pool pump
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class FilterPump : PumpModel
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    private const double Diff = -20.0;
    private const double Factor = 6 * 60;
    private const double Max = 237.0 * 60;
    private double _oldTemperature;

    public FilterPump()
    {
        Logger = Log.Logger?.ForContext<FilterPump>() ?? throw new ArgumentNullException(nameof(Logger));

        StartTriggerMorning = InitializeTrigger(StartTimerTriggered, Daily, GetTimerName(PoolControlHelper.GetPropertyName(() => StartTriggerMorning)));
        StartTriggerNoon = InitializeTrigger(StartTimerTriggered, Daily, GetTimerName(PoolControlHelper.GetPropertyName(() => StartTriggerNoon)));
        EndTriggerMorning = InitializeTrigger(EndTimerTriggered, Daily, GetTimerName(PoolControlHelper.GetPropertyName(() => EndTriggerMorning)));
        EndTriggerNoon = InitializeTrigger(EndTimerTriggered, Daily, GetTimerName(PoolControlHelper.GetPropertyName(() => EndTriggerNoon)));
        FilterOffTrigger = InitializeTrigger(EndTimerTriggered, Daily, GetTimerName(PoolControlHelper.GetPropertyName(() => FilterOffTrigger)));

        this.WhenAnyValue(x => x.StandardFilterRunTime).Subscribe(standardFilterTime => { publishMessage(PoolControlHelper.GetPropertyName(() => StandardFilterRunTime), standardFilterTime.ToString()); Recalculate(); });
        this.WhenAnyValue(x => x.StartMorning).Subscribe(startMorning => { publishMessage(PoolControlHelper.GetPropertyName(() => StartMorning), startMorning.ToString()); Recalculate(); });
        this.WhenAnyValue(x => x.StartNoon).Subscribe(startAfternoon => { publishMessage(PoolControlHelper.GetPropertyName(() => StartNoon), startAfternoon.ToString()); Recalculate(); });
        this.WhenAnyValue(x => x.FilterOff).Subscribe(filterOff => { publishMessage(PoolControlHelper.GetPropertyName(() => FilterOff), filterOff.ToString()); Recalculate(); });
        this.WhenAnyValue(x => x.NextStart)
            .Subscribe(nextStart => publishMessage(PoolControlHelper.GetPropertyName(() => NextStart),
                nextStart?.ToString(DateTimeFormat)));
        this.WhenAnyValue(x => x.NextEnd).Subscribe(nextEnd => publishMessage(PoolControlHelper.GetPropertyName(() => NextEnd), nextEnd?.ToString(DateTimeFormat)));
    }

    public override void OnTemperatureChange(MeasurementArgs args)
    {
        if (args.BaseMeasurement is not { ModelBase: not null }) return;
        PoolTemperature = (Temperature)args.BaseMeasurement.ModelBase!;
        // Only Recalculate if TemperatureChange ist greater than 1 °C
        if (!(Math.Abs(PoolTemperature!.Value - _oldTemperature) > 1)) return;
        Logger.Debug("Pool temperature change > 1 °C --> Recalculating");
        _oldTemperature = PoolTemperature.Value;
        Recalculate();
    }

    public void Recalculate()
    {
        if (PoolTemperature != null)
        {
            var secondsToAdd = (int)Math.Min(Max, Math.Max(StandardFilterRunTime * 60, StandardFilterRunTime * 60 + Factor * (PoolTemperature.Value + Diff)));
            Logger.Debug("New secondsToAdd To FilterRunTime: {SecondsToAdd}", secondsToAdd);
            startTrigger(FilterOffTrigger, FilterOff);
            startTrigger(StartTriggerMorning, StartMorning);
            startTrigger(StartTriggerNoon, StartNoon);
            startTrigger(EndTriggerMorning, StartMorning.Add(new TimeSpan(0, 0, 0, secondsToAdd)));
            startTrigger(EndTriggerNoon, StartNoon.Add(new TimeSpan(0, 0, 0, secondsToAdd)));
        }

        NextStart = StartTriggerMorning.TriggerTime.CompareTo(StartTriggerNoon.TriggerTime) < 1 ? StartTriggerMorning.TriggerTime : StartTriggerNoon.TriggerTime;
        NextEnd = EndTriggerMorning.TriggerTime.CompareTo(EndTriggerNoon.TriggerTime) < 1 ? EndTriggerMorning.TriggerTime : EndTriggerNoon.TriggerTime;
    }

    public override void RecalculateThings()
    {
        Recalculate();
    }

    protected override void OnTimerTicked(object? state)
    {
        // Nothing to do.
    }

    [JsonIgnore]
    protected Temperature? PoolTemperature { get; set; }

    [JsonIgnore]
    public TimeTrigger StartTriggerMorning { get; set; }

    [JsonIgnore]
    public TimeTrigger StartTriggerNoon { get; set; }

    [JsonIgnore]
    public TimeTrigger EndTriggerMorning { get; set; }

    [JsonIgnore]
    public TimeTrigger EndTriggerNoon { get; set; }

    [JsonIgnore]
    public TimeTrigger FilterOffTrigger { get; set; }

    [Reactive]
    [JsonProperty]
    public int StandardFilterRunTime { get; set; }

    [Reactive]
    [JsonProperty]
    public TimeSpan StartMorning { get; set; }

    [Reactive]
    [JsonProperty]
    public TimeSpan StartNoon { get; set; }

    [Reactive]
    [JsonProperty]
    public TimeSpan FilterOff { get; set; }

    [Reactive]
    [JsonProperty]
    public DateTime? NextStart { get; set; }

    [Reactive]
    [JsonProperty]
    public DateTime? NextEnd { get; set; }
}