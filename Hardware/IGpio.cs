using System.Device.Gpio;

namespace PoolControl.Hardware;

public interface IGpio
{
    int ReadPin(int pin);
    void Close(int pin);
    void Close(int[] pins, bool highIsOn);
    void DoSwitch(int pin, bool state, bool highIsOn);
    void DoSwitch(int[] pin, bool state, bool highIsOn);
    void Off(int pin, bool highIsOn);
    void Off(int[] pin, bool highIsOn);
    void On(int pin, bool highIsOn);
    void On(int[] pin, bool highIsOn);
    void Open(int pin, PinMode mode, bool highIsOn);
    void OpenPinModeInput(int pin, bool highIsOn);
    void OpenPinModeOutput(int pin, bool highIsOn);
    void OpenPinModeOutput(int[] pins, bool highIsOn);
    void Dispose();
}