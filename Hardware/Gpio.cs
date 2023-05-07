using Serilog;
using System;
using System.Device.Gpio;
using System.Runtime.InteropServices;
using Log = PoolControl.Helper.Log;

namespace PoolControl.Hardware;

public class Gpio : IGpio
{
    private static IGpio? _instance;
    private static readonly object Padlock = new object();

    protected ILogger Logger { get; private init; }
    private readonly GpioController _controller;

    private Gpio(ILogger? logger)
    {
        Logger = logger?.ForContext<Gpio>() ?? throw new ArgumentNullException(nameof(Logger));
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

    public int readPin(int pin)
    {
        return (int)_controller.Read(pin);
    }

    public void openPinModeOutput(int pin, bool highIsOn)
    {
        open(pin, PinMode.Output, highIsOn);
    }

    public void openPinModeInput(int pin, bool highIsOn)
    {
        open(pin, PinMode.Input, highIsOn);
    }

    public void open(int pin, PinMode mode, bool highIsOn)
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

    public void openPinModeOutput(int[] pins, bool highIsOn)
    {
        foreach (var t in pins)
        {
            openPinModeOutput(t, highIsOn);
        }
    }

    public void close(int pin)
    {
        if (pin < 1)
        {
            Logger.Warning("Error pin {Pin} close", pin);
            return;
        }
        Logger.Debug("Try to close {Pin}", pin);
        _controller.ClosePin(pin);
        Logger.Information("Closed {Pin}", pin);
    }

    public void close(int[] pins, bool highIsOn)
    {
        foreach (var t in pins)
        {
            close(t);
        }
    }

    public void doSwitch(int pin, bool state, bool highIsOn)
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
    public void doSwitch(int[] pin, bool state, bool highIsOn)
    {
        foreach (var t in pin)
        {
            doSwitch(t, state, highIsOn);
        }
    }


    public void on(int pin, bool highIsOn)
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

    public void on(int[] pin, bool highIsOn)
    {
        foreach (var t in pin)
        {
            on(t, highIsOn);
        }
    }

    public void off(int pin, bool highIsOn)
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
    public void off(int[] pin, bool highIsOn)
    {
        foreach (var t in pin)
        {
            off(t, highIsOn);
        }
    }

    public void dispose()
    {
        Logger.Debug("Try to dispose");
        _controller.Dispose();
        Logger.Information("Disposed");
    }
}