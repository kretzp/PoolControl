using System;
using Newtonsoft.Json;

namespace PoolControl.Hardware;

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

public enum MeasurementResultCode
{
    Success = 1,
    SyntaxError = 2,
    Pending = 254,
    NoDataToSend = 255
}