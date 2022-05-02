using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Helper;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Redox : EzoBase
    {
        public Redox()
        {
            Logger = Log.Logger?.ForContext<Redox>() ?? throw new ArgumentNullException(nameof(Logger));

            // Publish changes via MQTT
            this.WhenAnyValue(r => r.Aus).Subscribe((Action<int>)(ein => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => Aus), ein.ToString()); OnValueChange(); }));
            this.WhenAnyValue(r => r.Ein).Subscribe((Action<int>)(aus => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => Ein), aus.ToString()); OnValueChange(); }));
        }

        [Reactive]
        [JsonProperty]
        public int Ein { get; set; }

        [Reactive]
        [JsonProperty]
        public int Aus { get; set; }

        public override void OnValueChange()
        {
            if (Switch != null)
            {
                if (Value < Ein)
                {
                    Switch.On = true;
                    Logger.Information($"Ein({Switch.On}) Redox({Value:#0}) > Ein({Ein}");
                }
                else if (Value > Aus)
                {
                    Switch.On = false;
                    Logger.Information($"Ein({Switch.On}) Redox({Value:#0}) > Aus({Aus}");
                }
            }
        }
    }
}
