using System;
using System.Threading;
using System.Diagnostics;
using PoolControl.Helper;
using PoolControl.ViewModels;

namespace PoolControl.Hardware;

public class DistanceMeasurement : BaseMeasurement
{
    private static readonly TimeSpan EchoTimeout = TimeSpan.FromMilliseconds(50);
    private readonly IGpio? _gpio;
    private int _measurementInProgress;

    public DistanceMeasurement()
    {
        Logger = Log.Logger?.ForContext<DistanceMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
    }

    internal DistanceMeasurement(IGpio gpio) : this()
    {
        _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
    }

    private static double MeasureOneTime(IGpio gpio, int trigger, int echo, CancellationToken stopToken)
    {
        gpio.On(trigger, true);
        try
        {
            Thread.Sleep(1);
        }
        finally
        {
            gpio.Off(trigger, true);
        }

        var watch = Stopwatch.StartNew();

        while (gpio.ReadPin(echo) == 0)
        {
            stopToken.ThrowIfCancellationRequested();
            if (watch.Elapsed >= EchoTimeout)
                throw new TimeoutException("Distance sensor: no echo received within 50 ms.");
        }

        watch.Restart();
        while (gpio.ReadPin(echo) == 1)
        {
            stopToken.ThrowIfCancellationRequested();
            if (watch.Elapsed >= EchoTimeout)
                throw new TimeoutException("Distance sensor: echo remained high for more than 50 ms.");
        }

        // Monotonic timing is unaffected by wall-clock corrections.
        return watch.Elapsed.TotalSeconds * 34320 / 2;
    }

    protected override MeasurementResult DoMeasurement()
    {
        var result = new MeasurementResult
        {
            Device = GetType().Name, Command = "length",
            Result = -1,
            TimeStamp = DateTime.Now,
            ReturnCode = (int)MeasurementResultCode.Pending,
            StatusInfo = "Distance measurement already in progress."
        };
        // Skip overlapping callbacks rather than queueing measurements.
        if (Interlocked.CompareExchange(ref _measurementInProgress, 1, 0) != 0)
            return result;

        try
        {
            var model = ModelBase as Distance ?? throw new InvalidOperationException("Distance model is missing.");
            var count = model.NumberOfMeasurements;
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(model.NumberOfMeasurements), "At least one measurement is required.");

            var gpio = _gpio ?? Gpio.Instance;
            var (trigger, echo) = model.OpenedPins ?? (model.Trigger, model.Echo);
            var distance = 0.0;
            for (var i = 0; i < count; i++)
            {
                StopToken.ThrowIfCancellationRequested();
                distance += MeasureOneTime(gpio, trigger, echo, StopToken);
            }

            result.Result = distance / count;
            result.ReturnCode = (int)MeasurementResultCode.Success;
            result.StatusInfo = "OK";
        }
        catch (Exception ex)
        {
            Logger?.Error("Error: {Message}", ex.Message);
            result.ReturnCode = 99;
            result.StatusInfo = ex.Message;
        }
        finally
        {
            result.TimeStamp = DateTime.Now;
            Interlocked.Exchange(ref _measurementInProgress, 0);
        }

        return result;
    }
}
