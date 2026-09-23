using PoolControl.ViewModels;
using Serilog;
using System.Threading;

namespace PoolControl.Hardware;

public abstract class BaseMeasurement
{
    private readonly CancellationTokenSource _stopSource = new();
    protected CancellationToken StopToken => _stopSource.Token;
    public void RequestStop() => _stopSource.Cancel();

    protected ILogger? Logger { get; init; }

    public MeasurementModelBase? ModelBase { get; set; }

    protected abstract MeasurementResult DoMeasurement();

    public MeasurementResult Measure()
    {
        if (StopToken.IsCancellationRequested)
            return new MeasurementResult { ReturnCode = 99, StatusInfo = "Measurement stopped." };
        var result = DoMeasurement();

        if (StopToken.IsCancellationRequested)
        {
            result.ReturnCode = 99;
            result.StatusInfo = "Measurement stopped.";
            return result;
        }
        if (result.ReturnCode != 1) return result;
        if (ModelBase == null) return result;
            
        ModelBase.Value = result.Result;
        ModelBase.TimeStamp = result.TimeStamp;
        ModelBase.PublishMessageValue();

        return result;
    }
}
