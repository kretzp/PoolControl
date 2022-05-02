using Serilog;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoolControl.Hardware
{
    public class WinGpioMock : IGpio
    {
        private static IGpio? _instance;
        private static readonly object padlock = new object();

        protected ILogger Logger { get; private set; }

        private WinGpioMock(ILogger logger)
        {
            Logger = logger?.ForContext<WinGpioMock>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        public static IGpio Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new WinGpioMock(Log.Logger);
                    }
                    return _instance;
                }
            }
        }

        public void openPinModeOutput(int pin, bool highIsOn)
        {
            open(pin, PinMode.Output, highIsOn);
        }

        public void openPinModeOutput(int[] pins, bool highIsOn)
        {
            for (int i = 0; i < pins.Length; i++)
            {
                openPinModeOutput(pins[i], highIsOn);
            }
        }

        public void close(int pin)
        {
            if (pin == 0)
            {
                Logger.Warning("Try pin 0 close");
                return;
            }
            Logger.Debug($"Try to close {pin}");
            Logger.Information("Using WinGpioMock");
            Logger.Information($"Closed {pin}");
        }

        public void close(int[] pins, bool highIsOn)
        {
            for (int i = 0; i < pins.Length; i++)
            {
                close(pins[i]);
            }
        }

        public void doSwitch(int pin, bool state, bool highIsOn)
        {
            if (pin == 0)
            {
                Logger.Warning("Try pin 0 switch");
                return;
            }
            Logger.Debug($"Try state {state} highIsOn {highIsOn} pin {pin}");
            Logger.Information("Using WinGpioMock");
            Logger.Information($"state {state} highIsOn {highIsOn} pin {pin}");
        }
        public void doSwitch(int[] pin, bool state, bool highIsOn)
        {
            for (int i = 0; i < pin.Length; i++)
            {
                doSwitch(pin[i], state, highIsOn);
            }
        }


        public void on(int pin, bool highIsOn)
        {
            if (pin == 0)
            {
                Logger.Warning("Try pin 0 on");
                return;
            }
            Logger.Debug($"Try On {pin}");
            Logger.Information("Using WinGpioMock");
            Logger.Information($"On {pin}");
        }

        public void on(int[] pin, bool highIsOn)
        {
            for (int i = 0; i < pin.Length; i++)
            {
                on(pin[i], highIsOn);
            }
        }

        public void off(int pin, bool highIsOn)
        {
            if (pin == 0)
            {
                Logger.Warning("Try pin 0 off");
                return;
            }
            Logger.Debug($"Try Off {pin}");
            Logger.Information("Using WinGpioMock");
            Logger.Information($"Off {pin}");
        }
        public void off(int[] pin, bool highIsOn)
        {
            for (int i = 0; i < pin.Length; i++)
            {
                off(pin[i], highIsOn);
            }
        }

        public void dispose()
        {
            Logger.Debug("Try to dispose}");
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
            Logger.Debug($"Try to open {pin}");
            Logger.Information("Using WinGpioMock");
            Logger.Information($"Opened {pin} {mode}");
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
}
