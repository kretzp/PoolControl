using PoolControl.Hardware;
using Serilog;

namespace PoolControl.Helper
{
    internal static class Log
    {
        public static ILogger Logger { get; } = new LoggerConfiguration().CreateLogger();
    }
}

namespace PoolControl.ViewModels
{
    public class MeasurementModelBase
    {
        public string? Address { get; set; }
        public double Value { get; set; }
        public DateTime TimeStamp { get; set; }
        public virtual void PublishMessageValue() { }
    }

    public class EzoBase : MeasurementModelBase
    {
        public double Voltage { get; set; }
    }
}

namespace PoolControl.Tests
{
    internal sealed class RecordingEzoMeasurement : BaseEzoMeasurement
    {
        public string? ReceivedCommand { get; private set; }

        protected override MeasurementResult Send_i2c_command(string command)
        {
            ReceivedCommand = command;
            return new MeasurementResult
            {
                Command = command,
                ReturnCode = 1,
                StatusInfo = "OK"
            };
        }
    }

    internal static class Program
    {
        private static int Main()
        {
            try
            {
                CommandIsDispatchedThroughExplicitInterface();
                StoppedDeviceRejectsCommands();
                Console.WriteLine("EZO command regression tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void CommandIsDispatchedThroughExplicitInterface()
        {
            var measurement = new RecordingEzoMeasurement();
            IEzoCommandDevice device = measurement;

            var result = device.SendCommand("Cal,mid,7.00");

            Assert(measurement.ReceivedCommand == "Cal,mid,7.00", "The concrete sensor did not receive the command.");
            Assert(result.Command == "Cal,mid,7.00", "The command result was not returned.");
            Assert(result.ReturnCode == 1, "The command result code was changed.");
        }

        private static void StoppedDeviceRejectsCommands()
        {
            var measurement = new RecordingEzoMeasurement();
            measurement.RequestStop();

            AssertThrows<OperationCanceledException>(() => measurement.SendCommand("R"));
            Assert(measurement.ReceivedCommand is null, "A stopped sensor received a command.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
        }
    }
}
