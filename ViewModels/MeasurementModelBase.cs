    using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Globalization;
using System.Threading;
using PoolControl.Hardware;

namespace PoolControl.ViewModels
{
    // Base mode for all measurments
    [JsonObject(MemberSerialization.OptIn)]
    public class MeasurementModelBase : ViewModelBase, IDisposable
    {
        private const string A = "PoolControl.Hardware.";
        private const string V = "Measurement";

        public MeasurementModelBase()
        {
            // Send InotifyChanged to View for Label, FullText and Temperature with Unit
            this.WhenAnyValue(ds => ds.Name, ds => ds.Value, ds => ds.ViewFormat, ds => ds.UnitSign, (name, temperature, viewFormat, unitSign) => $"{LocationName}: {temperature.ToString(viewFormat)} {unitSign}").ToPropertyEx(this, ds => ds.FullText, deferSubscription: true);
            this.WhenAnyValue(ds => ds.Name, ds => ds.Value, (name, value) => $"{LocationName}:").ToPropertyEx(this, ds => ds.Label, deferSubscription: true);
            this.WhenAnyValue(ds => ds.Value, ds => ds.ViewFormat, ds => ds.UnitSign, (value, viewFormat, unitSign) => $"{value.ToString(viewFormat)} {unitSign}").ToPropertyEx(this, ds => ds.ValueWithUnit, deferSubscription: true);

            // RestartReading and Publish
            this.WhenAnyValue(ds => ds.IntervalInSec).Subscribe(value => this.RestartTimerAndPublishNewInterval());

            string name = GetType().Name;
            Type t = Type.GetType(A + name + V);

            BaseMeasurement = (BaseMeasurement?)Activator.CreateInstance(t);
            BaseMeasurement.ModelBase = this;
        }

        [JsonIgnore]
        public BaseMeasurement BaseMeasurement { get; set; }

        [Reactive]
        [JsonProperty]
        public double Value { get; set; }

        [Reactive]
        [JsonProperty]
        public string Address { get; set; }

        [Reactive]
        [JsonProperty]
        public string UnitSign { get; set; }

        [Reactive]
        [JsonProperty]
        public string ViewFormat { get; set; }

        [Reactive]
        [JsonProperty]
        public string InterfaceFormat { get; set; }

        [Reactive]
        [JsonProperty]
        public DateTime TimeStamp { get; set; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string FullText { get; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string Label { get; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string ValueWithUnit { get; }

        [JsonIgnore]
        public string InterfaceFormatLocal
        {
            get { return GetInterfaceFormatLocal(Value); }
        }

        [JsonIgnore]
        public string InterfaceFormatDecimalPoint
        {
            get { return GetInterfaceFormatDecimalPoint(Value);  }
        }

        public string GetInterfaceFormatDecimalPoint(double o)
        {
            return o.ToString(InterfaceFormat, new CultureInfo("en-US"));
        }

        public string GetInterfaceFormatLocal(double o)
        {
            return o.ToString(InterfaceFormat);
        }

        public virtual void publishMessageValue()
        {
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => Value), InterfaceFormatDecimalPoint);
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => TimeStamp), TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss"));
        }

        protected override void OnTimerTicked(object? state)
        {
            MeasurementResult mr = BaseMeasurement.Measure();

            if (mr.ReturnCode == (int)MeasurmentResultCode.SUCCESS)
            {
                Logger.Information($"Measurment succesful: Class: {GetType().Name} Value {Value} at {TimeStamp:yyyy-MM-dd HH:mm:ss-fff}, Status: {mr.StatusInfo}");
                MeasurmentTaken?.Invoke(new MeasurementArgs { MeasurementResult = mr, BaseMeasurement = BaseMeasurement });
            }
            else
            {
                Logger.Error($"Error: Code: {mr.ReturnCode}, Status: {mr.StatusInfo}");
            }
        }

        public event MeasurementHandler MeasurmentTaken;
    }


    public delegate void MeasurementHandler(MeasurementArgs args);

    public class MeasurementArgs : EventArgs
    {
        public MeasurementResult? MeasurementResult { get; set; }
        public BaseMeasurement? BaseMeasurement { get; set; }
    }
}