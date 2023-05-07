using System;
using System.Threading;
using PoolControl.Helper;
using PoolControl.ViewModels;

namespace PoolControl.Hardware;

public class DistanceMeasurement : BaseMeasurement
{
    public DistanceMeasurement()
    {
        Logger = Log.Logger?.ForContext<DistanceMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
    }

    private double MeasureOneTime()
    {
        // Set Trigger High
        Gpio.Instance.on(((Distance)ModelBase!).Trigger, true);

        // Set Trigger Low after 1 ms
        Thread.Sleep(1);
        Gpio.Instance.off(((Distance)ModelBase).Trigger, true);

        DateTime start = DateTime.Now;
        DateTime end = DateTime.Now;

        // Start/Stop time
        while (Gpio.Instance.readPin(((Distance)ModelBase).Echo) == 0)
        {
            start = DateTime.Now;
        }

        // Here is a possible memory leak, if sensor doesn't respond
        while (Gpio.Instance.readPin(((Distance)ModelBase).Echo) == 1)
        {
            end = DateTime.Now;
        }

        // Time gone
        var diff = end.Ticks - start.Ticks;

        // Calculate speed of sound (343,2 m / s)
        return diff * 0.003432 / 2;
    }

    private double MeasureMultipleTimes()
    {
        try
        {
            var distance = 0.0;
            var no = ((Distance)ModelBase!).NumberOfMeasurements;
            for (var i = 0; i < no; i++)
            {
                distance += MeasureOneTime();
            }

            return distance / no;
        }
        catch (Exception ex)
        {
            Logger?.Error("Error: {Message}", ex.Message);
        }

        return -1.0;
    }

    protected override MeasurementResult DoMeasurement()
    {
        var result = new MeasurementResult
        {
            Device = GetType().Name, Command = "length",
            Result = MeasureMultipleTimes(),
            TimeStamp = DateTime.Now
        };
        if (result.Result < 0)
        {
            result.ReturnCode = 99;
            result.StatusInfo = "Error";
        }
        else
        {
            result.ReturnCode = 1;
            result.StatusInfo = "OK";
        }

        return result;
    }
}