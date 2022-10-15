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
            this.WhenAnyValue(d => d.WinterMode).Subscribe(winterMode => SetWinterMode());
        }

        private void SetWinterMode()
        {
            if (Temperatures is not null)
            {
                foreach (var temperature in Temperatures)
                {
                    temperature.WinterMode = this.WinterMode;
                }
            }

            if (Switches is not null)
            {
                foreach (var sw in Switches)
                {
                    sw.WinterMode = this.WinterMode;
                    sw.On = false;
                }
            }

            if (FilterPump is not null)
            {
                FilterPump.WinterMode = this.WinterMode;
            }

            if (FilterPump is not null)
            {
                SolarHeater.WinterMode = this.WinterMode;
            }

            if (Ph is not null)
            {
                Ph.WinterMode = this.WinterMode;
            }

            if (Redox is not null)
            {
                Redox.WinterMode = this.WinterMode;
            }

            if (Distance is not null)
            {
                Distance.WinterMode = this.WinterMode;
            }
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
