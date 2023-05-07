using Serilog;
using System;
using System.Device.Gpio;
using Log = PoolControl.Helper.Log;

namespace PoolControl.Hardware;

public class WinGpioMock : IGpio
{
    private static IGpio? _instance;
    private static readonly object Padlock = new object();

    private ILogger Logger { get; init; }

    private WinGpioMock(ILogger? logger)
    {
        Logger = logger?.ForContext<WinGpioMock>() ?? throw new ArgumentNullException(nameof(logger));
    }

    public static IGpio Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new WinGpioMock(Log.Logger);
            }
        }
    }

    public void openPinModeOutput(int pin, bool highIsOn)
    {
        open(pin, PinMode.Output, highIsOn);
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
        if (pin == 0)
        {
            Logger.Warning("Try pin 0 close");
            return;
        }
        Logger.Debug("Try to close {Pin}", pin);
        Logger.Information("Using WinGpioMock");
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
        if (pin == 0)
        {
            Logger.Warning("Try pin 0 switch");
            return;
        }
        Logger.Debug("Try state {State} highIsOn {HighIsOn} pin {Pin}", state, highIsOn, pin);
        Logger.Information("Using WinGpioMock");
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
        if (pin == 0)
        {
            Logger.Warning("Try pin 0 on");
            return;
        }
        Logger.Debug("Try On {Pin}", pin);
        Logger.Information("Using WinGpioMock");
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
            Logger.Warning("Try pin 0 off");
            return;
        }
        Logger.Debug("Try Off {Pin}", pin);
        Logger.Information("Using WinGpioMock");
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
        Logger.Information("Using WinGpioMock");
        Logger.Information("Disposed");
    }

    public void open(int pin, PinMode mode, bool highIsOn)
    {
        if (pin == 0)
        {
            Logger.Warning("Try pin 0 open");
            return;
        }
        Logger.Debug("Try to open {Pin}", pin);
        Logger.Information("Using WinGpioMock");
        Logger.Information("Opened {Pin} {Mode}", pin, mode);
    }

    public void openPinModeInput(int pin, bool highIsOn)
    {
        open(pin, PinMode.Input, highIsOn);
    }

    public int readPin(int pin)
    {
        return (int)(DateTime.Now.Ticks % 2);
    }
}