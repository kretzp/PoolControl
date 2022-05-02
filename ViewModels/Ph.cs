using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Time;
using System.Threading;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Ph : EzoBase
    {
        public Ph()
        {
            Logger = Log.Logger?.ForContext<Ph>() ?? throw new ArgumentNullException(nameof(Logger));

            // Publish changes via MQTT
            this.WhenAnyValue(p => p.MaxValue).Subscribe((Action<double>)(value => publishMessageWithType(PoolControlHelper.GetPropertyName(() => MaxValue), PoolControlHelper.format1Decimal(value))));
            this.WhenAnyValue(p => p.Einlaufdauer).Subscribe(einlaufdauerinterval => RestartPhTimerAndPublishNewInterval());
            this.WhenAnyValue(p => p.EinlaufdauerInterval).Subscribe(einlaufdauerinterval => RestartPhTimerAndPublishNewInterval());
        }

        public void RestartPhTimerAndPublishNewInterval()
        {
            PhTimerEin = RestartTimer(PhTimerEin, CheckPh, PhInterval);
            PhTimerAus = RestartTimer(PhTimerAus, PhAus, PhInterval + 1000 * Einlaufdauer, PhInterval);

            publishMessageWithType(PoolControlHelper.GetPropertyName(() => Einlaufdauer), Einlaufdauer.ToString());
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => EinlaufdauerInterval), EinlaufdauerInterval.ToString());
        }

        private void PhAus(object? state)
        {
            if(Switch != null)
            {
                Switch.On = false;
                Logger.Information($"{Switch.Name} Ein({Switch.On})");
            }
        }

        protected void CheckPh(object? state)
        {
            if (Switch != null)
            {
                if (Value > MaxValue)
                {
                    Switch.On = true;
                    Logger.Information($"{Switch.Name} Ein({Switch.On}) pH({Value}) > MaxPh({MaxValue})");
                }
                else
                {
                    Logger.Information($"{Switch.Name} Ein({Switch.On}) pH({Value}) < MaxPh({MaxValue})");
                }     
            }
        }

        [JsonIgnore]
        protected Timer? PhTimerEin { get; private set; }

        [JsonIgnore]
        protected Timer? PhTimerAus { get; private set; }

        [JsonIgnore]
        public Temperature PoolTemperature { get; set; }

        [Reactive]
        [JsonProperty]
        public double MaxValue { get; set; }

        [Reactive]
        [JsonProperty]
        public int Einlaufdauer { get; set; }

        [Reactive]
        [JsonProperty]
        public int EinlaufdauerInterval { get; set; }

        [JsonIgnore]
        public int PhInterval { get { return EinlaufdauerInterval * 60 * 1000; } }

        public override void OnValueChange()
        {
            //throw new NotImplementedException();
        }
    }
}
