using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using PoolControl.Hardware;
using PoolControl.Helper;
using PoolControl.Time;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// Data for solar heater
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SolarHeater : PumpModel
    {
        public SolarHeater()
        {
            Logger = Log.Logger?.ForContext<SolarHeater>() ?? throw new ArgumentNullException(nameof(Logger));
            TurnOnTrigger = InitializeTrigger(StartTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => TurnOnTrigger)));
            TurnOffTrigger = InitializeTrigger(EndTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => TurnOffTrigger)));

            this.WhenAnyValue(x => x.TurnOnDiff).Subscribe(einschaltdiff => publishMessageWithType(PoolControlHelper.GetPropertyName(() => TurnOnDiff), PoolControlHelper.format1Decimal(einschaltdiff)));
            this.WhenAnyValue(x => x.TurnOffDiff).Subscribe(ausschaltdiff => publishMessageWithType(PoolControlHelper.GetPropertyName(() => TurnOffDiff), PoolControlHelper.format1Decimal(ausschaltdiff)));
            this.WhenAnyValue(x => x.MaxPoolTemp).Subscribe(maxPoolTemp => publishMessageWithType(PoolControlHelper.GetPropertyName(() => MaxPoolTemp), PoolControlHelper.format1Decimal(maxPoolTemp)));
            this.WhenAnyValue(x => x.SolarHeaterCleaningDuration).Subscribe(spueldauer => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => SolarHeaterCleaningDuration), spueldauer.ToString()); RecalculateSolarHeatingCleaning(); });
            this.WhenAnyValue(x => x.SolarHeaterCleaningTime).Subscribe(spuelzeitpunkt => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => SolarHeaterCleaningTime), spuelzeitpunkt.ToString()); RecalculateSolarHeatingCleaning(); });
        }

        public override void OnTemperatureChange(MeasurementArgs args)
        {
            Temperature temp = (Temperature)args.BaseMeasurement.ModelBase;
            if (temp.Key.Equals("SolarPreRun")) SolarPreLoopTemperature = temp;
            if (temp.Key.Equals("SolarHeater")) SolarHeaterTemperature = temp;

            if (SolarPreLoopTemperature == null || SolarHeaterTemperature == null)
            {
                Logger.Debug($"SolarPreLoopTemperature or SolarHeaterTemperature is null");
                return;
            }

            Logger.Debug($"SolarPreLoopTemperature: {SolarPreLoopTemperature.Value} SolarHeaterTemperature: {SolarHeaterTemperature.Value}");

            DateTime now = DateTime.Now;
            if (now < NextEnd && now > NextEnd - new TimeSpan(0, 0, SolarHeaterCleaningDuration))
            {
                Logger.Information("Do nothing because cleaning of solar heater is running");
                return;
            }

            if (SolarHeaterTemperature.Value > SolarPreLoopTemperature.Value + TurnOnDiff)
            {
                if (SolarPreLoopTemperature.Value < MaxPoolTemp)
                {
                    Switch.On = true;
                }
                else
                {
                    Switch.On = false;
                }
                Logger.Information($"{Switch.Name} On({Switch.On}) SolarHeater({SolarHeaterTemperature.Value:#0.0}) > Pool({SolarPreLoopTemperature.Value:#0.0}) + Ein({TurnOnDiff:#0.0}) = Sum({SolarPreLoopTemperature.Value:#0.0}) Max({MaxPoolTemp:#0.0})");

            }
            else if (SolarHeaterTemperature.Value < SolarPreLoopTemperature.Value + TurnOffDiff)
            {
                Switch.On = false;
                Logger.Information($"{Switch.Name} On({Switch.On}) SolarHeater({SolarHeaterTemperature.Value:#0.0}) > Pool({SolarPreLoopTemperature.Value:#0.0}) + Aus({TurnOffDiff:#0.0}) = Sum({SolarPreLoopTemperature.Value:#0.0}) Max({MaxPoolTemp:#0.0})");
            }
        }

        public void RecalculateSolarHeatingCleaning()
        {
            startTrigger(TurnOnTrigger, SolarHeaterCleaningTime);
            startTrigger(TurnOffTrigger, SolarHeaterCleaningTime.Add(new TimeSpan(0, 0, SolarHeaterCleaningDuration)));
            NextStart = TurnOnTrigger.TriggerTime;
            NextEnd = TurnOffTrigger.TriggerTime;
        }

        public override void RecalculateThings()
        {
            RecalculateSolarHeatingCleaning();   
        }

        [JsonIgnore]
        protected Temperature SolarPreLoopTemperature { get; set; }

        [JsonIgnore]
        protected Temperature SolarHeaterTemperature { get; set; }

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
}
