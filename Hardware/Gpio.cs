using Serilog;
using System;
using System.Device.Gpio;
using System.Runtime.InteropServices;
using Log = PoolControl.Helper.Log;

namespace PoolControl.Hardware;

public class Gpio : IGpio
{
    private static IGpio? _instance;
    private static readonly object Padlock = new();

    protected ILogger Logger { get; private init; }
    private readonly GpioController _controller;

    private Gpio(ILogger? logger)
    {
        Logger = logger?.ForContext<Gpio>() ?? throw new ArgumentException(nameof(Logger));
        _controller = new GpioController(PinNumberingScheme.Logical);
    }

    public static IGpio Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? WinGpioMock.Instance
                    : new Gpio(Log.Logger);
            }
        }
    }

    public int ReadPin(int pin)
    {
        return (int)_controller.Read(pin);
    }

    public void OpenPinModeOutput(int pin, bool highIsOn)
    {
        Open(pin, PinMode.Output, highIsOn);
    }

    public void OpenPinModeInput(int pin, bool highIsOn)
    {
        Open(pin, PinMode.Input, highIsOn);
    }

    public void Open(int pin, PinMode mode, bool highIsOn)
    {
        if (pin < 1)
        {
            Logger.Warning("Error pin {Pin} open", pin);
            return;
        }
        Logger.Debug("Try to open {Pin}", pin);
        _controller.OpenPin(pin, mode, highIsOn ? PinValue.Low : PinValue.High);
        Logger.Information("Opened {Pin}", pin);
    }

    public void OpenPinModeOutput(int[] pins, bool highIsOn)
    {
        foreach (var t in pins)
        {
            OpenPinModeOutput(t, highIsOn);
        }
    }

    public void Close(int pin)
    {
        if (pin < 1)
        {
            Logger.Warning("Error pin {Pin} Close", pin);
            return;
        }
        Logger.Debug("Try to Close {Pin}", pin);
        _controller.ClosePin(pin);
        Logger.Information("Closed {Pin}", pin);
    }

    public void Close(int[] pins, bool highIsOn)
    {
        foreach (var t in pins)
        {
            Close(t);
        }
    }

    public void DoSwitch(int pin, bool state, bool highIsOn)
    {
        if (pin < 1)
        {
            Logger.Warning("Try pin {Pin} switch, which is not allowed", pin);
            return;
        }
        Logger.Debug("Try state {State} highIsOn {HighIsOn} pin {Pin}", state, highIsOn, pin);
        _controller.Write(pin, highIsOn ? state : !state);
        Logger.Information("state {State} highIsOn {HighIsOn} pin {Pin}", state, highIsOn, pin);
    }
    public void DoSwitch(int[] pin, bool state, bool highIsOn)
    {
        foreach (var t in pin)
        {
            DoSwitch(t, state, highIsOn);
        }
    }


    public void On(int pin, bool highIsOn)
    {
        if (pin < 1)
        {
            Logger.Warning("Error pin {Pin} on", pin);
            return;
        }
        Logger.Debug("Try On {Pin}", pin);
        _controller.Write(pin, highIsOn ? PinValue.High : PinValue.Low);
        Logger.Information("On {Pin}", pin);
    }

    public void On(int[] pin, bool highIsOn)
    {
        foreach (var t in pin)
        {
            On(t, highIsOn);
        }
    }

    public void Off(int pin, bool highIsOn)
    {
        if (pin == 0)
        {
            Logger.Warning("Error pin {Pin} off", pin);
            return;
        }
        Logger.Debug("Try Off {Pin}", pin);
        _controller.Write(pin, highIsOn ? PinValue.Low : PinValue.High);
        Logger.Information("Off {Pin}", pin);
    }
    public void Off(int[] pin, bool highIsOn)
    {
        foreach (var t in pin)
        {
            Off(t, highIsOn);
        }
    }

    public void Dispose()
    {
        Logger.Debug("Try to dispose");
        _controller.Dispose();
        Logger.Information("Disposed");
    }
}