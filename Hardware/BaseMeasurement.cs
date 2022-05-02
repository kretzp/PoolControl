using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoolControl.ViewModels;
using Serilog;

namespace PoolControl.Hardware
{
    public abstract class BaseMeasurement
    {
        protected ILogger Logger { get; set; }

        public MeasurementModelBase ModelBase { get; set; }

        public abstract MeasurementResult DoMeasurement();

        public MeasurementResult Measure()
        {
            MeasurementResult result = DoMeasurement();

            if (result.ReturnCode == 1)
            {
                // Publish Message even if it is equal!!!
                if(ModelBase.Value == result.Result)
                {
                    ModelBase.publishMessageValue();
                }

                ModelBase.Value = result.Result;
                ModelBase.TimeStamp = result.TimeStamp;
            }

            return result;
        }
    }
}
