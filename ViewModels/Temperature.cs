using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using System;
using PoolControl.Helper;

namespace PoolControl.ViewModels;

/// <summary>
/// Data fpr all temperatures
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class Temperature : MeasurementModelBase
{
    public Temperature()
    {
        Logger = Log.Logger?.ForContext<Temperature>() ?? throw new ArgumentNullException(nameof(Logger));
    }

    public override void PublishMessageValue()
    {
        PublishMessage($"Temperatures/{Key}/Temperature", InterfaceFormatDecimalPoint, !String.IsNullOrEmpty(Key), false);
        PublishMessage($"Temperatures/{Key}/TimeStamp", TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss"), !String.IsNullOrEmpty(Key), false);
    }

    [Reactive]
    [JsonProperty]
    public string? Key { get; set; }
}