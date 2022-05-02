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
    [JsonObject(MemberSerialization.OptIn)]
    public class Solarheizung : PumpenModel
    {
        public Solarheizung()
        {
            Logger = Log.Logger?.ForContext<Solarheizung>() ?? throw new ArgumentNullException(nameof(Logger));
            TriggerEinschalten = InitializeTrigger(StartTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => TriggerEinschalten)));
            TriggerAusschalten = InitializeTrigger(EndeTimerTriggered, DAILY, GetTimerName(PoolControlHelper.GetPropertyName(() => TriggerAusschalten)));

            this.WhenAnyValue(x => x.EinschaltDiff).Subscribe((Action<double>)(einschaltdiff => publishMessageWithType(PoolControlHelper.GetPropertyName(() => EinschaltDiff), PoolControlHelper.format1Decimal(einschaltdiff))));
            this.WhenAnyValue(x => x.AusschaltDiff).Subscribe((Action<double>)(ausschaltdiff => publishMessageWithType(PoolControlHelper.GetPropertyName(() => AusschaltDiff), PoolControlHelper.format1Decimal(ausschaltdiff))));
            this.WhenAnyValue(x => x.MaxPoolTemp).Subscribe((Action<double>)(maxPoolTemp => publishMessageWithType(PoolControlHelper.GetPropertyName(() => MaxPoolTemp), PoolControlHelper.format1Decimal(maxPoolTemp))));
            this.WhenAnyValue(x => x.Spueldauer).Subscribe((Action<int>)(spueldauer => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => Spueldauer), spueldauer.ToString()); RecalculateSpuelzeitpunkt(); }));
            this.WhenAnyValue(x => x.Spuelzeitpunkt).Subscribe((Action<TimeSpan>)(spuelzeitpunkt => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => Spuelzeitpunkt), spuelzeitpunkt.ToString()); RecalculateSpuelzeitpunkt();
        }));
        }

        public override void OnTemperatureChange(MeasurementArgs args)
        {
            Temperature temp = (Temperature)args.BaseMeasurement.ModelBase;
            if (temp.Key.Equals("Solarzulauf")) SolarzulaufTemperature = temp;
            if (temp.Key.Equals("Solarheizung")) SolarheizungTemperature = temp;

            DateTime now = DateTime.Now;
            if (now < NextEnd && now > NextEnd - new TimeSpan(0, 0, Spueldauer))
            {
                Logger.Information("Do nothing because Spülung läuft");
                return;
            }

            if (SolarheizungTemperature != null && SolarzulaufTemperature != null)
            {
                if (SolarheizungTemperature.Value > SolarzulaufTemperature.Value + EinschaltDiff)
                {
                    if (SolarzulaufTemperature.Value < MaxPoolTemp)
                    {
                        Switch.On = true;
                    }
                    else
                    {
                        Switch.On = false;
                    }
                    Logger.Information($"{Switch.Name} Ein({Switch.On}) Solarheizung({SolarheizungTemperature.Value:#0.0}) > Pool({SolarzulaufTemperature.Value:#0.0}) + Ein({EinschaltDiff:#0.0}) = Summe({SolarzulaufTemperature.Value:#0.0}) Max({MaxPoolTemp:#0.0})");

                }
                else if (SolarheizungTemperature.Value < SolarzulaufTemperature.Value + AusschaltDiff)
                {
                    Switch.On = false;
                    Logger.Information($"{Switch.Name} Ein({Switch.On}) Solarheizung({SolarheizungTemperature.Value:#0.0}) > Pool({SolarzulaufTemperature.Value:#0.0}) + Aus({AusschaltDiff:#0.0}) = Summe({SolarzulaufTemperature.Value:#0.0}) Max({MaxPoolTemp:#0.0})");
                }
            }
        }

        public void RecalculateSpuelzeitpunkt()
        {
            startTrigger(TriggerEinschalten, Spuelzeitpunkt);
            startTrigger(TriggerAusschalten, Spuelzeitpunkt.Add(new TimeSpan(0, 0, Spueldauer)));
            NextStart = TriggerEinschalten.TriggerTime;
            NextEnd = TriggerAusschalten.TriggerTime;
        }

        public override void RecalculateThings()
        {
            RecalculateSpuelzeitpunkt();   
        }

        [JsonIgnore]
        protected Temperature SolarzulaufTemperature { get; set; }

        [JsonIgnore]
        protected Temperature SolarheizungTemperature { get; set; }

        [JsonIgnore]
        public TimeTrigger TriggerEinschalten { get; set; }
        
        [JsonIgnore]
        public TimeTrigger TriggerAusschalten { get; set; }

        [Reactive]
        [JsonProperty]
        public TimeSpan Spuelzeitpunkt { get; set; }

        [Reactive]
        [JsonProperty]
        public double EinschaltDiff { get; set; }

        [Reactive]
        [JsonProperty]
        public double AusschaltDiff { get; set; }

        [Reactive]
        [JsonProperty]
        public double MaxPoolTemp   { get; set; }

        [Reactive]
        [JsonProperty]
        public int Spueldauer { get; set; }

        [Reactive]
        [JsonProperty]
        public DateTime NextStart { get; set; }

        [Reactive]
        [JsonProperty]
        public DateTime NextEnd { get; set; }
    }
}
