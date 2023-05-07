using System.Device.Gpio;

namespace PoolControl.Hardware;

public interface IGpio
{
    int readPin(int pin);
    void close(int pin);
    void close(int[] pins, bool highIsOn);
    void doSwitch(int pin, bool state, bool highIsOn);
    void doSwitch(int[] pin, bool state, bool highIsOn);
    void off(int pin, bool highIsOn);
    void off(int[] pin, bool highIsOn);
    void on(int pin, bool highIsOn);
    void on(int[] pin, bool highIsOn);
    void open(int pin, PinMode mode, bool highIsOn);
    void openPinModeInput(int pin, bool highIsOn);
    void openPinModeOutput(int pin, bool highIsOn);
    void openPinModeOutput(int[] pins, bool highIsOn);
    void dispose();
}