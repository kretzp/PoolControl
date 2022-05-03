using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Time;
using PoolControl.Helper;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// This class ist used to hold the data of filter pump, the pool pump
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class FilterPump : PumpModel
    {
        private const double diff = -20.0;
        private const double factor = 6 * 60;
        private const double max = 237.0 * 60;
        private double oldTemperature;

        public FilterPump()
        {
            Logger = Log.Logger?.ForContext<FilterPump>() ?? throw new ArgumentNullException(nameof(Logger));

            StartTriggerMorning = InitializeTrigger(StartTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => StartTriggerMorning)));
            StartTriggerNoon = InitializeTrigger(StartTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => StartTriggerNoon)));
            EndTriggerMorning = InitializeTrigger(EndTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => EndTriggerMorning)));
            EndTriggerNoon = InitializeTrigger(EndTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => EndTriggerNoon)));

            this.WhenAnyValue(x => x.StandardFilterRunTime).Subscribe(standardFilterlaufzeit => { publishMessage(PoolControlHelper.GetPropertyName(() => StandardFilterRunTime), standardFilterlaufzeit.ToString()); Recalculate(); });
            this.WhenAnyValue(x => x.StartMorning).Subscribe(startVormittags => { publishMessage(PoolControlHelper.GetPropertyName(() => StartMorning), startVormittags.ToString()); Recalculate(); });
            this.WhenAnyValue(x => x.StartNoon).Subscribe(startNachmittags => { publishMessage(PoolControlHelper.GetPropertyName(() => StartNoon), startNachmittags.ToString()); Recalculate(); });
            this.WhenAnyValue(x => x.NextStart).Subscribe(nextStart => publishMessage(PoolControlHelper.GetPropertyName(() => NextStart), nextStart.ToString()));
            this.WhenAnyValue(x => x.NextEnd).Subscribe(nextEnd => publishMessage(PoolControlHelper.GetPropertyName(() => NextEnd), nextEnd.ToString()));
        }

        public override void OnTemperatureChange(MeasurementArgs args)
        {
            PoolTemperature = (Temperature)args.BaseMeasurement.ModelBase;
            // Only Recalculate if TemperaturChange ist greater than 1 °C
            if (PoolTemperature != null && Math.Abs(PoolTemperature.Value - oldTemperature) > 1)
            {
                Logger.Debug("Pool temperature change > 1 °C --> Recalculating");
                oldTemperature = PoolTemperature.Value;
                Recalculate();
            }
        }

        public void Recalculate()
        {
            int secondsToAdd = (int)Math.Min(max, Math.Max(StandardFilterRunTime * 60, StandardFilterRunTime * 60 + factor * (PoolTemperature == null?0:PoolTemperature.Value + diff)));
            Logger.Debug($"New scondsToAdd To Filterlaufzeit: {secondsToAdd}");
            startTrigger(StartTriggerMorning, StartMorning);
            startTrigger(StartTriggerNoon, StartNoon);
            startTrigger(EndTriggerMorning, StartMorning.Add(new TimeSpan(0, 0, 0, secondsToAdd)));
            startTrigger(EndTriggerNoon, StartNoon.Add(new TimeSpan(0, 0, 0, secondsToAdd)));
            NextStart = StartTriggerMorning.TriggerTime.CompareTo(StartTriggerNoon.TriggerTime) < 1 ? StartTriggerMorning.TriggerTime : StartTriggerNoon.TriggerTime;
            NextEnd = EndTriggerMorning.TriggerTime.CompareTo(EndTriggerNoon.TriggerTime) < 1 ? EndTriggerMorning.TriggerTime : EndTriggerNoon.TriggerTime;
        }

        public override void RecalculateThings()
        {
            Recalculate();
        }

        [JsonIgnore]
        protected Temperature PoolTemperature { get; set; }

        [JsonIgnore]
        public TimeTrigger StartTriggerMorning { get; set; }

        [JsonIgnore]
        public TimeTrigger StartTriggerNoon { get; set; }

        [JsonIgnore]
        public TimeTrigger EndTriggerMorning { get; set; }

        [JsonIgnore]
        public TimeTrigger EndTriggerNoon { get; set; }

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
        public DateTime NextStart { get; set; }

        [Reactive]
        [JsonProperty]
        public DateTime NextEnd { get; set; }
    }
}
