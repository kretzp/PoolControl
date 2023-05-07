using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using PoolControl.Helper;

namespace PoolControl.Hardware;


[UsedImplicitly]
public class RedoxMeasurement : BaseEzoMeasurement
{
    private int _i;
    private bool _add = true;
    private double _redox = 730;

    public RedoxMeasurement()
    {
        Logger = Log.Logger?.ForContext<RedoxMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
    }

    protected override MeasurementResult send_i2c_command(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (command.ToLower().Equals("status"))
            {
                return new MeasurementResult { Result = 3.84, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = "?STATUS,P,3.84", Device = $"{GetType().Name}.{ModelBase?.Name}@{I2CAddress}", Command = "Status" };
            }
            else
            {
                _i++;

                var a = _i % 10;
                if (a == 0) _add = !_add;
                var m = _add ? 1 : -1;
                _redox += m * 15;

                var code = $"Using WinBaseEZOMock: Class {this.GetType().Name} Command {command} Address {I2CAddress}";
                Logger?.Information("{Code}", code);
                return new MeasurementResult { Result = _redox, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = code, Device = $"{GetType().Name}.{ModelBase?.Name}@{I2CAddress}", Command = "ph" };
            }
        }

        return base.send_i2c_command(command);
    }

    public MeasurementResult calibrate(int value)
    {
        return send_i2c_command("Cal," + value);
    }
}