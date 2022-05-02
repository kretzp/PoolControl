using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PoolControl.Hardware
{
    public class RedoxMeasurement : BaseEzoMeasurement
    {
        int i = 0;
        bool add = true;
        double redox = 730;

        public RedoxMeasurement()
        {
            base.Logger = Log.Logger.ForContext<RedoxMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        public override MeasurementResult send_i2c_command(string command)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MeasurementResult ezoResult = new MeasurementResult { ReturnCode = (int)MeasurmentResultCode.PENDING, StatusInfo = "Error: ", Result = double.NaN, Device = $"{GetType().Name}@{I2CAddress}", Command = command };

                i++;

                int a = i % 10;
                if (a == 0) add = !add;
                int m = add ? 1 : -1;
                redox += m * 15;

                string code = $"Using WinBaseEZOMock: Class {this.GetType().Name} Command {command} Address {I2CAddress}";
                Logger.Information(code);
                return new MeasurementResult { Result = redox, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = code, Device = $"{GetType().Name}.{ModelBase.Name}@{I2CAddress}", Command = "ph" };
            }

            return base.send_i2c_command(command);
        }

        public MeasurementResult calibrate(int value)
        {
            return send_i2c_command("Cal," + value);
        }
    }
}
