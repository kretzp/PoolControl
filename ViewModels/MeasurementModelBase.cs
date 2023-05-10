    using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Globalization;
using PoolControl.Hardware;
    using PoolControl.Helper;

    namespace PoolControl.ViewModels; 

    // Base mode for all measurements
    [JsonObject(MemberSerialization.OptIn)]
    public class MeasurementModelBase : ViewModelBase
    {
        private const string A = "PoolControl.Hardware.";
        private const string V = "Measurement";

        public MeasurementModelBase()
        {
            // Send InotifyChanged to View for Label, FullText and Temperature with Unit
            this.WhenAnyValue(ds => ds.Name, ds => ds.Value, ds => ds.ViewFormat, ds => ds.UnitSign, (_, temperature, viewFormat, unitSign) => $"{LocationName}: {temperature.ToString(viewFormat)} {unitSign}").ToPropertyEx(this, ds => ds.FullText, deferSubscription: true);
            this.WhenAnyValue(ds => ds.Name, ds => ds.Value, (_, _) => $"{LocationName}:").ToPropertyEx(this, ds => ds.Label, deferSubscription: true);
            this.WhenAnyValue(ds => ds.Value, ds => ds.ViewFormat, ds => ds.UnitSign, (value, viewFormat, unitSign) => $"{value.ToString(viewFormat)} {unitSign}").ToPropertyEx(this, ds => ds.ValueWithUnit, deferSubscription: true);

            // RestartReading and Publish
            this.WhenAnyValue(ds => ds.IntervalInSec).Subscribe(_ => this.RestartTimerAndPublishNewInterval());

            var name = GetType().Name;
            var t = Type.GetType(A + name + V);

            if (t != null) BaseMeasurement = (BaseMeasurement?)Activator.CreateInstance(t);
            if (BaseMeasurement != null) BaseMeasurement.ModelBase = this;
        }

        [JsonIgnore]
        public BaseMeasurement? BaseMeasurement { get; set; }

        [Reactive]
        [JsonProperty]
        public double Value { get; set; }

        [Reactive]
        [JsonProperty]
        public string? Address { get; set; }

        [Reactive]
        [JsonProperty]
        public string? UnitSign { get; set; }

        [Reactive]
        [JsonProperty]
        public string? ViewFormat { get; set; }

        [Reactive]
        [JsonProperty]
        public string? InterfaceFormat { get; set; }

        [Reactive]
        [JsonProperty]
        public DateTime TimeStamp { get; set; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string? FullText { get; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string? Label { get; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string? ValueWithUnit { get; }

        [JsonIgnore]
        public string InterfaceFormatLocal => GetInterfaceFormatLocal(Value);

        [JsonIgnore]
        public string InterfaceFormatDecimalPoint => GetInterfaceFormatDecimalPoint(Value);

        public string GetInterfaceFormatDecimalPoint(double o)
        {
            return o.ToString(InterfaceFormat, new CultureInfo("en-US"));
        }

        public string GetInterfaceFormatLocal(double o)
        {
            return o.ToString(InterfaceFormat);
        }

        public virtual void PublishMessageValue()
        {
            PublishMessageWithType(PoolControlHelper.GetPropertyName(() => Value), InterfaceFormatDecimalPoint, false);
            PublishMessageWithType(PoolControlHelper.GetPropertyName(() => TimeStamp), TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss"), false);
        }

        protected override void OnTimerTicked(object? state)
        {
            var mr = BaseMeasurement?.Measure();

            if (mr is { ReturnCode: (int)MeasurementResultCode.Success })
            {
                Logger.Information("{Message}", $"Measurement successful: Class: {GetType().Name} Value {Value} at {TimeStamp:yyyy-MM-dd HH:mm:ss-fff}, Status: {mr.StatusInfo}");
                MeasurementTaken?.Invoke(new MeasurementArgs { BaseMeasurement = BaseMeasurement });
            }
            else
            {
                if (mr != null) Logger.Error("Error: Code: {RetCode}, Status: {StatInf}", mr.ReturnCode, mr.StatusInfo);
            }
        }

        public event MeasurementHandler? MeasurementTaken;
    }


    public delegate void MeasurementHandler(MeasurementArgs args);

    public class MeasurementArgs : EventArgs
    {
        public BaseMeasurement? BaseMeasurement { get; init; }
    }