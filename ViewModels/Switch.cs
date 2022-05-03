using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using PoolControl.Hardware;


namespace PoolControl.ViewModels
{
    /// <summary>
    /// Data for all Switches
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Switch : ViewModelBase
    {
        public Switch()
        {
            Logger = Log.Logger?.ForContext<Switch>() ?? throw new ArgumentNullException(nameof(Logger));

            this.WhenAnyValue(s => s.On).Subscribe(on => switchRelay());
        }

        [Reactive]
        public string Key { get; set; }

        [Reactive]
        public int RelayNumber { get; set; }

        [Reactive]
        public bool HighIsOn { get; set; }

        [Reactive]
        public bool On { get; set; }

        public void switchRelay()
        {
            Logger.Debug($"Switch {Key} {On} changed");
            Gpio.Instance.doSwitch(RelayConfig.Instance.GetGpioForRelayNumber(RelayNumber), On, HighIsOn);
            base.publishMessage($"Switches/{Key}/On", On ? "1" : "0", 2, true, !String.IsNullOrEmpty(Key));
        }
    }
}
