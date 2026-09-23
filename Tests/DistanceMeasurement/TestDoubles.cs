// Compile the production measurement sources without constructing UI models,
// timers, MQTT clients or real GPIO controllers.
namespace PoolControl.Helper
{
    public static class Log
    {
        public static Serilog.ILogger Logger { get; } = new Serilog.LoggerConfiguration().CreateLogger();
    }
}
namespace PoolControl.ViewModels
{
    public class MeasurementModelBase
    {
        public double Value { get; set; } = 42;
        public DateTime TimeStamp { get; set; }
        public int Publications { get; private set; }
        public void PublishMessageValue() => Publications++;
    }
    public class Distance : MeasurementModelBase
    {
        public (int Trigger, int Echo)? OpenedPins { get; set; }
        public int Trigger => 16;
        public int Echo => 26;
        public int NumberOfMeasurements { get; set; } = 1;
    }
}
namespace PoolControl.Hardware
{
    public static class Gpio
    {
        public static IGpio Instance => throw new InvalidOperationException("Tests must inject their GPIO instance.");
    }
}
