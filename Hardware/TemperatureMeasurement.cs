using System;
using System.IO;
using PoolControl.ViewModels;
using System.Runtime.InteropServices;
using PoolControl.Helper;

namespace PoolControl.Hardware;

public class TemperatureMeasurement : BaseMeasurement
{
    private string Ds18B20Address => @"/sys/bus/w1/devices/" + ModelBase?.Address + @"/w1_slave";
    private const string WindowsDs18B20Address = "w1_slave";

    private int _i;
    private double _temp = 25.1;
    private const double PoolTemp = 25.9;
    private bool _add = true;

    public TemperatureMeasurement()
    {
        Logger = Log.Logger?.ForContext<TemperatureMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
    }

    private MeasurementResult emulateWindowsTemp()
    {
        _i++;
        var retValue = PoolTemp;

        if (ModelBase is { Name: "SolarHeater" })
        {
            var a = _i % 10;
            if (a == 0) _add = !_add;
            var m = _add ? 1 : -1;
            _temp += m * 1;
            retValue = _temp;
        }

        var code = $"Using WinBaseEZOMock: Sensor: {ModelBase?.Name} Temperature: {retValue}";
        Logger?.Information("{Code}", code);
        return new MeasurementResult { Result = retValue, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = code, Device = $"{GetType().Name}.{ModelBase?.Name}@{Ds18B20Address}", Command = "temperature" };
    }

    protected override MeasurementResult DoMeasurement()
    {
        var mr = new MeasurementResult { Device = $"{GetType().Name}.{ModelBase?.Name}", Command = "temperature" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {

        }

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        try
        {
            var content = File.ReadAllLines(isWindows?WindowsDs18B20Address:Ds18B20Address);
            foreach (var t in content)
            {
                Logger?.Debug("{T}",t);
            }
            var tempData = content[1].Split(new[] { ' ' }, StringSplitOptions.None)[9][2..];
            Logger?.Debug("{TempData}", tempData);
            mr.TimeStamp = DateTime.Now;
            mr.Result = double.Parse(tempData) / 1000;
            mr.StatusInfo = "OK";
            mr.ReturnCode = 1;
            Logger?.Debug("Temperature {Temp}", this.GetType().Name);
            switch (mr.Result)
            {
                case > 80:
                    throw new ArgumentException("Error in DS18B20, Temperature > 80 °C");
                case 0:
                    throw new ArgumentException("Error in DS18B20, Temperature > 0 °C");
            }
        }
        catch (Exception ex)
        {
            Logger?.Error("Error in getting temperature: Key: {Temp}, Name: {Name}", ((Temperature)ModelBase!).Key, ModelBase.Name);
            Logger?.Error("{Message}", ex.Message);
            Logger?.Verbose(ex, "Verbose Error Logging");
            mr.Result = double.NaN;
            mr.ReturnCode = 99;
            mr.StatusInfo = ex.Message;
        }

        if(isWindows)
        {
            mr = emulateWindowsTemp();
        }

        return mr;
    }
}