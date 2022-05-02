using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PoolControl.Hardware
{
    [JsonObject(MemberSerialization.OptOut)]
    public class MeasurementResult
    {
        public int ReturnCode { get; set; }

        public double Result { get; set; }

        public DateTime TimeStamp { get; set; }

        public string? StatusInfo { get; set; }

        public string? Device { get; set; }

        public string? Command { get; set; }
    }

    public enum MeasurmentResultCode : int
    {
        SUCCESS = 1,
        SYNTAXERROR = 2,
        PENDING = 254,
        NODATATOSEND = 255
    }
}
