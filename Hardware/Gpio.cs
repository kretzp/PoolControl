using Serilog;
using System;
using System.Device.Gpio;
using System.Runtime.InteropServices;

namespace PoolControl.Hardware
{
    public class Gpio : IGpio
    {
        private static IGpio? _instance;
        private static readonly object padlock = new object();

        protected ILogger Logger { get; private set; }
        protected GpioController controller;

        private Gpio(ILogger logger)
        {
            Logger = logger?.ForContext<Gpio>() ?? throw new ArgumentNullException(nameof(Logger));
            controller = new GpioController(PinNumberingScheme.Logical);
        }

        public static IGpio Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            _instance = WinGpioMock.Instance; 
                        }
                        else
                        {
                            _instance = new Gpio(Log.Logger);
                        }
                    }
                    return _instance;
                }
            }
        }

        public int readPin(int pin)
        {
            return (int)controller.Read(pin);
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
                Logger.Warning($"Error pin {pin} open");
                return;
            }
            Logger.Debug($"Try to open {pin}");
            controller.OpenPin(pin, mode, highIsOn ? PinValue.Low : PinValue.High);
            Logger.Information($"Opened {pin}");
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
            if (pin < 1)
            {
                Logger.Warning($"Error pin {pin} close");
                return;
            }
            Logger.Debug($"Try to close {pin}");
            controller.ClosePin(pin);
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
            if (pin < 1)
            {
                Logger.Warning($"Try pin {pin} switch, which is not allowed");
                return;
            }
            Logger.Debug($"Try state {state} highIsOn {highIsOn} pin {pin}");
            controller.Write(pin, highIsOn ? state : !state);
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
            if (pin < 1)
            {
                Logger.Warning($"Error pin {pin} on");
                return;
            }
            Logger.Debug($"Try On {pin}");
            controller.Write(pin, highIsOn ? PinValue.High : PinValue.Low);
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
                Logger.Warning($"Error pin {pin} off");
                return;
            }
            Logger.Debug($"Try Off {pin}");
            controller.Write(pin, highIsOn ? PinValue.Low : PinValue.High);
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
            controller.Dispose();
            Logger.Information("Disposed");
        }
    }
}
