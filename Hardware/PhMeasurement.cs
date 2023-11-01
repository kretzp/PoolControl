using PoolControl.ViewModels;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using PoolControl.Helper;

namespace PoolControl.Hardware;

public class PhMeasurement : BaseEzoMeasurement
{
    public decimal? Temperature { get; set; }

    private int _i;
    private bool _add = true;
    private double _ph = 6.9;

    public PhMeasurement()
    {
        Logger = Log.Logger?.ForContext<PhMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
    }

    protected override MeasurementResult Send_i2c_command(string command)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return base.Send_i2c_command(command);
        if (command.ToLower().Equals("status"))
        {
            return new MeasurementResult { Result = 3.85, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = "?STATUS,P,3.85", Device = $"{GetType().Name}.{ModelBase?.Name}@{I2CAddress}", Command = "Status" };
        }
        else
        {
            _i++;

            var a = _i % 10;
            if (a == 0) _add = !_add;
            var m = _add ? 1 : -1;
            _ph += m * 0.1;

            var code = $"Using WinBaseEZOMock: Class {this.GetType().Name} Command {command} Address {I2CAddress}";
            Logger?.Information("{Code}", code);
            return new MeasurementResult { Result = _ph, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = code, Device = $"{GetType().Name}.{ModelBase?.Name}@{I2CAddress}", Command = "ph" };
        }

    }


    public MeasurementResult MidCalibration(double where)
    {
        return Calibrate("mid", where);
    }

    public MeasurementResult LowCalibration(double where)
    {
        return Calibrate("low", where);
    }

    public MeasurementResult HighCalibration(double where)
    {
        return Calibrate("high", where);
    }

    private MeasurementResult Calibrate(string where, double what)
    {
        return Send_i2c_command(String.Format(new CultureInfo("en-US"), "Cal,{0},{1:#0.00}", where, what));
    }

    public MeasurementResult Slope()
    {
        return Send_i2c_command("Slope,?");
    }

    public MeasurementResult ExtendedPhScaleOn()
    {
        return Send_i2c_command("pHext,1");
    }

    public MeasurementResult ExtendedPhScaleOff()
    {
        return Send_i2c_command("pHext,0");
    }

    public MeasurementResult GetExtendedPhScale()
    {
        return Send_i2c_command("pHext,?");
    }

    public MeasurementResult GetTemperatureCompensation()
    {
        return Send_i2c_command("T,?");
    }

    public MeasurementResult SetTemperatureCompensation(decimal temperature)
    {
        return Send_i2c_command(String.Format(new CultureInfo("en-US"), "T,{0:#0.0}", temperature));
    }

    private MeasurementResult TakeReadingTemperatureCompensation(decimal? temperature)
    {
        if (temperature != null)
        {
            return Send_i2c_command(String.Format(new CultureInfo("en-US"), "RT,{0:#0.0}", temperature));
        }
        else
        {
            return TakeReading();
        }
    }

    protected override MeasurementResult DoMeasurement()
    {
        MeasurementResult vmr = DoVoltageMeasurement();
        ((EzoBase)ModelBase!).Voltage = vmr.Result;

        return TakeReadingTemperatureCompensation(Temperature);
    }
}