using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Hardware;
using PoolControl.Helper;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class EzoBase : MeasurementModelBase
    {
        public EzoBase()
        {
            // Change LED and publish Message
            this.WhenAnyValue(r => r.LedOn).Subscribe(ledon => SwitchLEDAndPublishMessage());
        }

        public abstract void OnValueChange();

        public override void publishMessageValue()
        {
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => Value), InterfaceFormatDecimalPoint);
            OnValueChange();
        }

        protected void SwitchLEDAndPublishMessage()
        {
            new BaseEzoMeasurement { ModelBase = this }.switchLedState(LedOn);
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => LedOn), LedOn ? "1" : "0");
        }

        [JsonIgnore]
        public Switch Switch { get; set; }

        [Reactive]
        [JsonProperty]
        public bool LedOn { get; set; }
    }
}
