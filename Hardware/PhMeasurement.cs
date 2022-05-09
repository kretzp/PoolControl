using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PoolControl.Hardware
{
    public class PhMeasurement : BaseEzoMeasurement
    {
        public decimal? Temperature { get; set; }

        int i = 0;
        bool add = true;
        double ph = 6.9;

        public PhMeasurement()
        {
            Logger = Log.Logger.ForContext<PhMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
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
                ph += m * 0.1;

                string code = $"Using WinBaseEZOMock: Class {this.GetType().Name} Command {command} Address {I2CAddress}";
                Logger.Information(code);
                return new MeasurementResult { Result = ph, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = code, Device = $"{GetType().Name}.{ModelBase.Name}@{I2CAddress}", Command = "ph" };
            }

            return base.send_i2c_command(command);
        }


        public MeasurementResult midCalibration(double where)
        {
            return calibrate("mid", where);
        }

        public MeasurementResult lowCalibration(double where)
        {
            return calibrate("low", where);
        }

        public MeasurementResult highCalibration(double where)
        {
            return calibrate("high", where);
        }

        protected MeasurementResult calibrate(string where, double what)
        {
            return send_i2c_command(String.Format(new CultureInfo("en-US"), "Cal,{0},{1:#0.00}", where, what));
        }

        public MeasurementResult slope()
        {
            return send_i2c_command("Slope,?");
        }

        public MeasurementResult extendedPhScaleOn()
        {
            return send_i2c_command("pHext,1");
        }

        public MeasurementResult extendedPhScaleOff()
        {
            return send_i2c_command("pHext,0");
        }

        public MeasurementResult getExtendedPhScale()
        {
            return send_i2c_command("pHext,?");
        }

        public MeasurementResult getTemperatureCompensation()
        {
            return send_i2c_command("T,?");
        }

        public MeasurementResult setTemperatureCompensation(decimal temperature)
        {
            return send_i2c_command(String.Format(new CultureInfo("en-US"), "T,{0:#0.0}", temperature));
        }

        public MeasurementResult takeReadingTemperatureCompensation(decimal? temperature)
        {
            if (temperature != null)
            {
                return send_i2c_command(String.Format(new CultureInfo("en-US"), "RT,{0:#0.0}", temperature));
            }
            else
            {
                return takeReading();
            }
        }

        public override MeasurementResult DoMeasurement()
        {
            MeasurementResult mr = takeReadingTemperatureCompensation(Temperature);
            if (mr.Result <= 0)
            {
                throw new ArgumentOutOfRangeException("Error in pH  <= 0");
            }

            return mr;
        }
    }
}
