using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// Data fpr all temperatures
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Temperature : MeasurementModelBase
    {
        public Temperature() : base()
        {
            Logger = Log.Logger?.ForContext<Temperature>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        public override void publishMessageValue()
        {
            publishMessage($"Temperatures/{Key}/Temperature", InterfaceFormatDecimalPoint, !String.IsNullOrEmpty(Key));
        }

        [Reactive]
        [JsonProperty]
        public string Key { get; set; }
    }
}