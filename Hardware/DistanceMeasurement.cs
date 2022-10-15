using System;
using System.Threading;
using PoolControl.ViewModels;

namespace PoolControl.Hardware
{
    public class DistanceMeasurement : BaseMeasurement
    {
        public DistanceMeasurement()
        {
            Logger = Log.Logger.ForContext<DistanceMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        private double MeasureOneTime()
        {
            // Trigger High setzen
            Gpio.Instance.on(((Distance)ModelBase).Trigger, true);

            // Trigger Low setzen nach 1 ms
            Thread.Sleep(1);
            Gpio.Instance.off(((Distance)ModelBase).Trigger, true);

            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;

            // Start/Stopzeit ermitteln
            while (Gpio.Instance.readPin(((Distance)ModelBase).Echo) == 0)
            {
                start = DateTime.Now;
            }

            // Here is a possible memory leak, if sensor doesn't respond
            while (Gpio.Instance.readPin(((Distance)ModelBase).Echo) == 1)
            {
                end = DateTime.Now;
            }

            // Vergangene Zeit
            long diff = end.Ticks - start.Ticks;

            // Schallgeschwindigkeit(343,2 m / s) einbeziehen
            return diff * 0.003432 / 2;
        }

        private double MeasureMultipleTimes()
        {
            try
            {
                double entfernung = 0.0;
                int no = ((Distance)ModelBase).NumberOfMeasurements;
                for (int i = 0; i < no; i++)
                {
                    entfernung += MeasureOneTime();
                }

                return entfernung / no;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

            return -1.0;
        }

        public override MeasurementResult DoMeasurement()
        {
            MeasurementResult result = new MeasurementResult { Device = GetType().Name, Command = "length" };
            result.Result = MeasureMultipleTimes();
            result.TimeStamp = DateTime.Now;
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
}
