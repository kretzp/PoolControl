using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Helper;

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

            // Publish changes via MQTT
            this.WhenAnyValue(r => r.Off).Subscribe(ein => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => Off), ein.ToString()); OnValueChange(); });
            this.WhenAnyValue(r => r.On).Subscribe(aus => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => On), aus.ToString()); OnValueChange(); });
        }

        [Reactive]
        [JsonProperty]
        public int On { get; set; }

        [Reactive]
        [JsonProperty]
        public int Off { get; set; }

        public override void OnValueChange()
        {
            if (Switch != null)
            {
                if (Value < On)
                {
                    Switch.On = true;
                    Logger.Information($"Ein({Switch.On}) Redox({Value:#0}) > Ein({On}");
                }
                else if (Value > Off)
                {
                    Switch.On = false;
                    Logger.Information($"Ein({Switch.On}) Redox({Value:#0}) > Aus({Off}");
                }
            }
        }
    }
}
