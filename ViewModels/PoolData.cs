using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using PoolControl.Hardware;
using PoolControl.Helper;

namespace PoolControl.ViewModels;

/// <summary>
/// All PoolData which will be loaded via json at the beginning
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class PoolData : ViewModelBase
{
    public PoolData()
    {
        Logger = Log.Logger?.ForContext<PoolData>() ?? throw new ArgumentNullException(nameof(Logger));
        this.WhenAnyValue(d => d.WinterMode).Subscribe(_ => SetWinterMode());
    }

    private void SetWinterMode()
    {
        if (Temperatures is not null)
        {
            foreach (var temperature in Temperatures)
            {
                temperature.WinterMode = WinterMode;
            }
        }

        if (Switches is not null)
        {
            foreach (var sw in Switches)
            {
                sw.WinterMode = WinterMode;
                sw.On = false;
            }
        }

        if (FilterPump is not null)
        {
            FilterPump.WinterMode = WinterMode;
        }

        if (FilterPump is not null)
        {
            if (SolarHeater != null) SolarHeater.WinterMode = WinterMode;
        }

        if (Ph is not null)
        {
            Ph.WinterMode = WinterMode;
        }

        if (Redox is not null)
        {
            Redox.WinterMode = WinterMode;
        }

        if (Distance is not null)
        {
            Distance.WinterMode = WinterMode;
        }
    }

    [Reactive]
    public List<Temperature>? Temperatures { get; set; }

    [JsonProperty("Temperatures")]
    public Dictionary<string, Temperature>? TemperaturesDict { get; set; }
    public Dictionary<string, object>? TemperaturesObj { get; set; }

    [Reactive]
    public List<Switch>? Switches { get; set; }

    [JsonProperty(nameof(Switches))]
    public Dictionary<string, Switch>? SwitchesDict { get; set; }
    public Dictionary<string, object>? SwitchesObj { get; set; }

    [JsonProperty]
    public RelayConfig? RelayConfig { get; set; }

    [Reactive]
    [JsonProperty]
    public FilterPump? FilterPump { get; set; }

    [Reactive]
    [JsonProperty]
    public SolarHeater? SolarHeater { get; set; }

    [Reactive]
    [JsonProperty]
    public Ph? Ph { get; set; }

    [Reactive]
    [JsonProperty]
    public Redox? Redox { get; set; }

    [Reactive]
    [JsonProperty]
    public Distance? Distance { get; set; }

    public void OpenGpioEchoAndTrigger()
    {
        if (Distance == null) return;
        Gpio.Instance.openPinModeOutput(Distance.Trigger, true);
        Gpio.Instance.openPinModeInput(Distance.Echo, true);
    }

    public void CloseGpioEchoAndTrigger()
    {
        if (Distance == null) return;
        Gpio.Instance.close(Distance.Trigger);
        Gpio.Instance.close(Distance.Echo);
    }

    public void OpenGpioSwitches()
    {
        if (Switches == null) return;
        foreach (var sw in Switches.Where(_ => RelayConfig != null))
        {
            if (RelayConfig != null)
                Gpio.Instance.openPinModeOutput(RelayConfig.GetGpioForRelayNumber(sw.RelayNumber), sw.HighIsOn);
        }
    }

    public void CloseGpioSwitches()
    {
        if (Switches == null) return;
        foreach (var sw in Switches.Where(_ => RelayConfig != null))
        {
            if (RelayConfig != null) Gpio.Instance.close(RelayConfig.GetGpioForRelayNumber(sw.RelayNumber));
        }
    }

    public void GpioSwitchesOff()
    {
        if (Switches == null) return;
        foreach (var sw in Switches.Where(_ => RelayConfig != null))
        {
            if (RelayConfig != null)
                Gpio.Instance.off(RelayConfig.GetGpioForRelayNumber(sw.RelayNumber), sw.HighIsOn);
        }
    }

    protected override void OnTimerTicked(object? state)
    {
        // Nothing to do
    }
}