namespace PoolControl.Hardware;

public interface IEzoCommandDevice
{
    MeasurementResult SendCommand(string command);
}
