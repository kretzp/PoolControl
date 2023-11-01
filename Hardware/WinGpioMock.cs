using Serilog;
using System;
using System.Device.Gpio;
using Log = PoolControl.Helper.Log;

namespace PoolControl.Hardware;

public class WinGpioMock : IGpio
{
    private static IGpio? _instance;
    private static readonly object Padlock = new();

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

    public void OpenPinModeOutput(int pin, bool highIsOn)
    {
        Open(pin, PinMode.Output, highIsOn);
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
        if (pin == 0)
        {
            Logger.Warning("Try pin 0 Close");
            return;
        }
        Logger.Debug("Try to Close {Pin}", pin);
        Logger.Information("Using WinGpioMock");
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
        if (pin == 0)
        {
            Logger.Warning("Try pin 0 switch");
            return;
        }
        Logger.Debug("Try state {State} highIsOn {HighIsOn} pin {Pin}", state, highIsOn, pin);
        Logger.Information("Using WinGpioMock");
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
        if (pin == 0)
        {
            Logger.Warning("Try pin 0 on");
            return;
        }
        Logger.Debug("Try On {Pin}", pin);
        Logger.Information("Using WinGpioMock");
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
            Logger.Warning("Try pin 0 off");
            return;
        }
        Logger.Debug("Try Off {Pin}", pin);
        Logger.Information("Using WinGpioMock");
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
        Logger.Information("Using WinGpioMock");
        Logger.Information("Disposed");
    }

    public void Open(int pin, PinMode mode, bool highIsOn)
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

    public void OpenPinModeInput(int pin, bool highIsOn)
    {
        Open(pin, PinMode.Input, highIsOn);
    }

    public int ReadPin(int pin)
    {
        return (int)(DateTime.Now.Ticks % 2);
    }
}