using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Helper;
using System.Reactive;
using PoolControl.Hardware;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// Data for Redox Measurment for salting engine
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Redox : EzoBase
    {
        public Redox()
        {
            Logger = Log.Logger?.ForContext<Redox>() ?? throw new ArgumentNullException(nameof(Logger));

            OnCal = ReactiveCommand.Create(Cal_Button_Clicked);

            // Publish changes via MQTT
            this.WhenAnyValue(r => r.Off).Subscribe(ein => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => Off), ein.ToString()); OnValueChange(); });
            this.WhenAnyValue(r => r.On).Subscribe(aus => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => On), aus.ToString()); OnValueChange(); });
        }

        private void Cal_Button_Clicked()
        {
            ((RedoxMeasurement)BaseMeasurement).calibrate(Cal);
        }

        public ReactiveCommand<Unit, Unit> OnCal { get; }

        [Reactive]
        [JsonProperty]
        public int On { get; set; }

        [Reactive]
        [JsonProperty]
        public int Off { get; set; }

        [Reactive]
        [JsonProperty]
        public int Cal { get; set; }

        public override void OnValueChange()
        {
            if(WinterMode)
            {
                Logger.Information($"Wintermode: Ein({Switch.On}) Redox({Value:#0})");
            }

            if (Switch != null)
            {
                if (FilterPumpSwitch.On && Value < On)
                {
                    Switch.On = true;
                    Logger.Information($"Ein({Switch.On}) Redox({Value:#0}) > Ein({On}) FilterPumP({FilterPumpSwitch.On})");
                }
                else if (!FilterPumpSwitch.On || Value > Off)
                {
                    Switch.On = false;
                    Logger.Information($"Ein({Switch.On}) Redox({Value:#0}) > Aus({Off}) FilterPumP({FilterPumpSwitch.On})");
                }
            }
        }
    }
}
