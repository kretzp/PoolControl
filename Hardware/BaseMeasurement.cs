using PoolControl.ViewModels;
using Serilog;

namespace PoolControl.Hardware;

public abstract class BaseMeasurement
{
    protected ILogger? Logger { get; init; }

    public MeasurementModelBase? ModelBase { get; set; }

    protected abstract MeasurementResult DoMeasurement();

    public MeasurementResult Measure()
    {
        var result = DoMeasurement();

        if (result.ReturnCode != 1) return result;
        if (ModelBase == null) return result;
            
        ModelBase.publishMessageValue();
        ModelBase.Value = result.Result;
        ModelBase.TimeStamp = result.TimeStamp;

        return result;
    }
}