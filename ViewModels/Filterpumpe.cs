using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Time;
using PoolControl.Helper;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Filterpumpe : PumpenModel
    {
        private const double diff = -20.0;
        private const double factor = 6 * 60;
        private const double max = 237.0 * 60;
        private double oldTemperature;

        public Filterpumpe()
        {
            Logger = Log.Logger?.ForContext<Filterpumpe>() ?? throw new ArgumentNullException(nameof(Logger));

            StartTriggerVormittags = InitializeTrigger(StartTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => StartTriggerVormittags)));
            StartTriggerNachmittags = InitializeTrigger(StartTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => StartTriggerNachmittags)));
            EndeTriggerVormittags = InitializeTrigger(EndeTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => EndeTriggerVormittags)));
            EndeTriggerNachmittags = InitializeTrigger(EndeTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => EndeTriggerNachmittags)));

            this.WhenAnyValue(x => x.StandardFilterlaufzeit).Subscribe((Action<int>)(standardFilterlaufzeit => { publishMessage(PoolControlHelper.GetPropertyName(() => StandardFilterlaufzeit), standardFilterlaufzeit.ToString()); Recalculate(); }));
            this.WhenAnyValue(x => x.StartVormittags).Subscribe((Action<TimeSpan>)(startVormittags => { publishMessage(PoolControlHelper.GetPropertyName(() => StartVormittags), startVormittags.ToString()); Recalculate(); }));
            this.WhenAnyValue(x => x.StartNachmittags).Subscribe((Action<TimeSpan>)(startNachmittags => { publishMessage(PoolControlHelper.GetPropertyName(() => StartNachmittags), startNachmittags.ToString()); Recalculate(); }));
            this.WhenAnyValue(x => x.NextStart).Subscribe((Action<DateTime>)(nextStart => publishMessage(PoolControlHelper.GetPropertyName(() => NextStart), nextStart.ToString())));
            this.WhenAnyValue(x => x.NextEnd).Subscribe((Action<DateTime>)(nextEnd => publishMessage(PoolControlHelper.GetPropertyName(() => NextEnd), nextEnd.ToString())));
        }

        public override void OnTemperatureChange(MeasurementArgs args)
        {
            PoolTemperature = (Temperature)args.BaseMeasurement.ModelBase;
            // Only Recalculate if TemperaturChange ist greater than 1 °C
            if (PoolTemperature != null && Math.Abs(PoolTemperature.Value - oldTemperature) > 1)
            {
                Logger.Debug("Pooltemperature change > 1 °C --> Recalculating");
                oldTemperature = PoolTemperature.Value;
                Recalculate();
            }
        }

        public void Recalculate()
        {
            int secondsToAdd = (int)Math.Min(max, Math.Max(StandardFilterlaufzeit * 60, StandardFilterlaufzeit * 60 + factor * (PoolTemperature == null?0:PoolTemperature.Value + diff)));
            Logger.Debug($"New scondsToAdd To Filterlaufzeit: {secondsToAdd}");
            startTrigger(StartTriggerVormittags, StartVormittags);
            startTrigger(StartTriggerNachmittags, StartNachmittags);
            startTrigger(EndeTriggerVormittags, StartVormittags.Add(new TimeSpan(0, 0, 0, secondsToAdd)));
            startTrigger(EndeTriggerNachmittags, StartNachmittags.Add(new TimeSpan(0, 0, 0, secondsToAdd)));
            NextStart = StartTriggerVormittags.TriggerTime.CompareTo(StartTriggerNachmittags.TriggerTime) < 1 ? StartTriggerVormittags.TriggerTime : StartTriggerNachmittags.TriggerTime;
            NextEnd = EndeTriggerVormittags.TriggerTime.CompareTo(EndeTriggerNachmittags.TriggerTime) < 1 ? EndeTriggerVormittags.TriggerTime : EndeTriggerNachmittags.TriggerTime;
        }

        public override void RecalculateThings()
        {
            Recalculate();
        }

        [JsonIgnore]
        protected Temperature PoolTemperature { get; set; }

        [JsonIgnore]
        public TimeTrigger StartTriggerVormittags { get; set; }

        [JsonIgnore]
        public TimeTrigger StartTriggerNachmittags { get; set; }

        [JsonIgnore]
        public TimeTrigger EndeTriggerVormittags { get; set; }

        [JsonIgnore]
        public TimeTrigger EndeTriggerNachmittags { get; set; }

        [Reactive]
        [JsonProperty]
        public int StandardFilterlaufzeit { get; set; }

        [Reactive]
        [JsonProperty]
        public TimeSpan StartVormittags { get; set; }

        [Reactive]
        [JsonProperty]
        public TimeSpan StartNachmittags { get; set; }

        [Reactive]
        [JsonProperty]
        public DateTime NextStart { get; set; }

        [Reactive]
        [JsonProperty]
        public DateTime NextEnd { get; set; }
    }
}
