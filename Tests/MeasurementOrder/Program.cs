using PoolControl.Hardware;
using PoolControl.ViewModels;

void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var expectedTime = new DateTime(2026, 9, 23, 12, 34, 56, DateTimeKind.Local);
var model = new MeasurementModelBase { Value = 12.5, TimeStamp = DateTime.MinValue };
var sensor = new FixedMeasurement(new MeasurementResult
{
    ReturnCode = (int)MeasurementResultCode.Success,
    Result = 42.75,
    TimeStamp = expectedTime
}) { ModelBase = model };

var result = sensor.Measure();

Assert(result.ReturnCode == (int)MeasurementResultCode.Success, "Successful result was changed.");
Assert(model.Value == 42.75 && model.TimeStamp == expectedTime, "Model was not updated from the measurement.");
Assert(model.PublishedValue == 42.75 && model.PublishedTime == expectedTime,
    "Publishing observed the previous measurement instead of the new one.");
Assert(model.Publications == 1, "Successful measurement was not published exactly once.");
Console.WriteLine("PASS: measurement values and timestamp are updated before publication");

var failedModel = new MeasurementModelBase { Value = 7 };
var failedSensor = new FixedMeasurement(new MeasurementResult { ReturnCode = 99, Result = 99 })
    { ModelBase = failedModel };
failedSensor.Measure();
Assert(failedModel.Value == 7 && failedModel.Publications == 0,
    "Failed measurement changed or published the model.");
Console.WriteLine("PASS: failed measurements neither overwrite nor publish state");

internal sealed class FixedMeasurement(MeasurementResult result) : BaseMeasurement
{
    protected override MeasurementResult DoMeasurement() => result;
}

namespace PoolControl.ViewModels
{
    public class MeasurementModelBase
    {
        public double Value { get; set; }
        public DateTime TimeStamp { get; set; }
        public int Publications { get; private set; }
        public double PublishedValue { get; private set; }
        public DateTime PublishedTime { get; private set; }

        public void PublishMessageValue()
        {
            Publications++;
            PublishedValue = Value;
            PublishedTime = TimeStamp;
        }
    }
}
