using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using PoolControl.Hardware;
using PoolControl.Helper;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// All PoolData which will be loaded via json at the beginning
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class PoolData : ViewModelBase
    {
        public PoolData()
        {
            Logger = Log.Logger?.ForContext<PoolData>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        [Reactive]
        public List<Temperature> Temperatures { get; set; }

        [JsonProperty("Temperatures")]
        public Dictionary<string, Temperature> TemperaturesDict { get; set; }
        public Dictionary<string, object> TemperaturesObj { get; set; }

        [Reactive]
        public List<Switch> Switches { get; set; }

        [JsonProperty("Switches")]
        public Dictionary<string, Switch> SwitchesDict { get; set; }
        public Dictionary<string, object> SwitchesObj { get; set; }

        [JsonProperty]
        public RelayConfig RelayConfig { get; set; }

        [Reactive]
        [JsonProperty]
        public FilterPump FilterPump { get; set; }

        [Reactive]
        [JsonProperty]
        public SolarHeater SolarHeater { get; set; }

        [Reactive]
        [JsonProperty]
        public Ph Ph { get; set; }

        [Reactive]
        [JsonProperty]
        public Redox Redox { get; set; }

        [Reactive]
        [JsonProperty]
        public Distance Distance { get; set; }

        public void OpenGpioEchoAndTrigger()
        {
            Gpio.Instance.openPinModeOutput(Distance.Trigger, true);
            Gpio.Instance.openPinModeInput(Distance.Echo, true);
        }

        public void CloseGpioEchoAndTrigger()
        {
            Gpio.Instance.close(Distance.Trigger);
            Gpio.Instance.close(Distance.Echo);
        }

        public void OpenGpioSwitches()
        {
            foreach(Switch sw in Switches)
            {
                Gpio.Instance.openPinModeOutput(RelayConfig.GetGpioForRelayNumber(sw.RelayNumber), sw.HighIsOn);
            }
        }

        public void CloseGpioSwitches()
        {
            foreach (Switch sw in Switches)
            {
                Gpio.Instance.close(RelayConfig.GetGpioForRelayNumber(sw.RelayNumber));
            }
        }

        public void GpioSwitchesOff()
        {
            foreach (Switch sw in Switches)
            {
                Gpio.Instance.off(RelayConfig.GetGpioForRelayNumber(sw.RelayNumber), sw.HighIsOn);
            }
        }

        protected override void OnTimerTicked(object? state)
        {
            throw new NotImplementedException();
        }
    }
}
