using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Hardware;
using PoolControl.Helper;
using System.Reactive;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// This is the base class for all Ezo Products Data
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class EzoBase : MeasurementModelBase
    {
        public EzoBase()
        {
            OnFind = ReactiveCommand.Create(Find_Button_Clicked);
            OnCalibrated = ReactiveCommand.Create(Calibrated_Button_Clicked);
            OnClearCalibration = ReactiveCommand.Create(ClearCalibrated_Button_Clicked);

            // Change LED and publish Message
            this.WhenAnyValue(r => r.LedOn).Subscribe(ledon => SwitchLEDAndPublishMessage());
            this.WhenAnyValue(r => r.Voltage).Subscribe(voltage => { publishMessageWithType(PoolControlHelper.GetPropertyName(() => Voltage), GetInterfaceFormatDecimalPoint(voltage)); });
        }

        private void ClearCalibrated_Button_Clicked()
        {
            ((BaseEzoMeasurement)BaseMeasurement).clearCalibration();
        }

        private void Calibrated_Button_Clicked()
        {
            SensorsCalibrated = (int)((BaseEzoMeasurement)BaseMeasurement).deviceCalibrated().Result;
        }

        private void Find_Button_Clicked()
        {
            ((BaseEzoMeasurement)BaseMeasurement).findDevice();
        }

        public abstract void OnValueChange();

        public override void publishMessageValue()
        {
            base.publishMessageValue();
            OnValueChange();
        }

        protected void SwitchLEDAndPublishMessage()
        {
            new BaseEzoMeasurement { ModelBase = this }.switchLedState(LedOn);
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => LedOn), LedOn ? "1" : "0");
        }

        public ReactiveCommand<Unit, Unit> OnFind { get; }

        public ReactiveCommand<Unit, Unit> OnCalibrated { get; }

        public ReactiveCommand<Unit, Unit> OnClearCalibration { get; }


        [JsonIgnore]
        public Switch Switch { get; set; }

        [JsonIgnore]
        public Switch FilterPumpSwitch { get; set; }

        [Reactive]
        [JsonProperty]
        public bool LedOn { get; set; }

        [Reactive]
        [JsonProperty]
        public int SensorsCalibrated { get; set; }

        [Reactive]
        [JsonProperty]
        public double Voltage { get; set; }
    }
}
