using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Hardware;
using System.Reactive;
using PoolControl.Helper;

namespace PoolControl.ViewModels;

/// <summary>
/// This is the base class for all Ezo Products Data
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public abstract class EzoBase : MeasurementModelBase
{
    protected EzoBase()
    {
        OnFind = ReactiveCommand.Create(Find_Button_Clicked);
        OnCalibrated = ReactiveCommand.Create(Calibrated_Button_Clicked);
        OnClearCalibration = ReactiveCommand.Create(ClearCalibrated_Button_Clicked);

        // Change LED and publish Message
        this.WhenAnyValue(r => r.LedOn).Subscribe(_ => SwitchLedAndPublishMessage());
        this.WhenAnyValue(r => r.Voltage).Subscribe(voltage => { PublishMessageWithType(PoolControlHelper.GetPropertyName(() => Voltage), GetInterfaceFormatDecimalPoint(voltage), false); });
    }

    private void ClearCalibrated_Button_Clicked()
    {
        ((BaseEzoMeasurement)BaseMeasurement!).ClearCalibration();
    }

    private void Calibrated_Button_Clicked()
    {
        SensorsCalibrated = (int)((BaseEzoMeasurement)BaseMeasurement!).DeviceCalibrated().Result;
    }

    private void Find_Button_Clicked()
    {
        ((BaseEzoMeasurement)BaseMeasurement!).FindDevice();
    }

    public abstract void OnValueChange();

    public override void PublishMessageValue()
    {
        base.PublishMessageValue();
        OnValueChange();
    }

    protected void SwitchLedAndPublishMessage()
    {
        new BaseEzoMeasurement { ModelBase = this }.SwitchLedState(LedOn);
        PublishMessageWithType(PoolControlHelper.GetPropertyName(() => LedOn), LedOn ? "1" : "0", true);
    }

    public ReactiveCommand<Unit, Unit> OnFind { get; }

    public ReactiveCommand<Unit, Unit> OnCalibrated { get; }

    public ReactiveCommand<Unit, Unit> OnClearCalibration { get; }


    [JsonIgnore]
    public Switch? Switch { get; set; }

    [JsonIgnore]
    public Switch? FilterPumpSwitch { get; set; }

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