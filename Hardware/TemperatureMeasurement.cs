using System;
using System.IO;
using PoolControl.ViewModels;
using System.Runtime.InteropServices;

namespace PoolControl.Hardware
{
    public class TemperatureMeasurement : BaseMeasurement
    {
        protected string DS18B20Address { get { return @"/sys/bus/w1/devices/" + ModelBase.Address + @"/w1_slave"; } }

        int i = 0;
        double temp = 25.1;
        double pooltemp = 25.9;
        bool add = true;

        public TemperatureMeasurement()
        {
            Logger = Log.Logger.ForContext<TemperatureMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        public override MeasurementResult DoMeasurement()
        {
            var mr = new MeasurementResult { Device = $"{GetType().Name}.{ModelBase.Name}", Command = "temperature" };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                i++;
                double retValue = pooltemp;

                if (ModelBase.Name.Equals("SolarHeater"))
                {
                    int a = i % 10;
                    if (a == 0) add = !add;
                    int m = add ? 1 : -1;
                    temp += m * 1;
                    retValue = temp;
                }

                string code = $"Using WinBaseEZOMock: Sensor: {ModelBase.Name} Temperature: {retValue}";
                Logger.Information(code);
                return new MeasurementResult { Result = retValue, ReturnCode = 1, TimeStamp = DateTime.Now, StatusInfo = code, Device = $"{GetType().Name}.{ModelBase.Name}@{DS18B20Address}", Command = "temperature" };
            }

            try
            {
                string[] inhalt = File.ReadAllLines(DS18B20Address);
                for (int i = 0; i < inhalt.Length; i++)
                {
                    Logger.Debug(inhalt[i]);
                }
                var tempdata = inhalt[1].Split(new char[] { ' ' }, StringSplitOptions.None)[9].Substring(2);
                Logger.Debug(tempdata);
                mr.TimeStamp = DateTime.Now;
                mr.Result = double.Parse(tempdata) / 1000;
                mr.StatusInfo = "OK";
                mr.ReturnCode = 1;
                Logger.Debug($"Temperature {mr.Result}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in getting temperature: Key: {((Temperature)ModelBase).Key}, Name: {ModelBase.Name}");
                Logger.Error(ex.Message);
                Logger.Verbose(ex, "Verbose Error Logging");
                mr.Result = double.NaN;
                mr.ReturnCode = 99;
                mr.StatusInfo = ex.Message;
            }

            return mr;
        }
    }
}
