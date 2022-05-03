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

            // publish Value
            this.WhenAnyValue(ds => ds.Value).Subscribe(value => this.publishMessageValue());

            // RestartReading and Publish
            this.WhenAnyValue(ds => ds.IntervalInSec).Subscribe(value => this.RestartTimerAndPublishNewInterval());

            string name = GetType().Name;
            Type t = Type.GetType(A + name + V);

            BaseMeasurement = (BaseMeasurement?)Activator.CreateInstance(t);
            BaseMeasurement.ModelBase = this;
        }

        [JsonIgnore]
        public BaseMeasurement BaseMeasurement { get; set; }

        [JsonIgnore]
        protected Timer? Timer { get; private set; }

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

        [Reactive]
        [JsonProperty]
        public int IntervalInSec { get; set; }

        [JsonIgnore]
        public int Interval { get { return IntervalInSec * 1000; } }

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
            get { return Value.ToString(InterfaceFormat); }
        }

        [JsonIgnore]
        public string InterfaceFormatDecimalPoint
        {
            get { return Value.ToString(InterfaceFormat, new CultureInfo("en-US")); }
        }

        public void RestartTimerAndPublishNewInterval()
        {
            Timer = RestartTimer(Timer, Read, Interval);

            publishMessageWithType(PoolControlHelper.GetPropertyName(() => IntervalInSec), IntervalInSec.ToString());
        }

        protected Timer RestartTimer(Timer timer, TimerCallback callback, int interval)
        {
            return RestartTimer(timer, callback, interval, interval);
        }

        protected Timer RestartTimer(Timer timer, TimerCallback callback, int duetime, int interval)
        {
            if (timer == null && interval > 0)
            {
                var autoEvent = new AutoResetEvent(false);
                timer = new Timer(callback, autoEvent, duetime, interval);
            }
            else
            {
                if (interval <= 0)
                {
                    if (Timer != null)
                    {
                        timer.Dispose();
                        timer = null;
                        Logger.Debug("Timer deleted");
                    }
                }
                else
                {
                    if (timer.Change(duetime, interval))
                    {
                        Logger.Debug($"Timer set: duetime {duetime} interval {interval}");
                    }
                    else
                    {
                        Logger.Debug($"Timer set: duetime {duetime} interval {interval} error");
                    }
                }
            }

            return timer;
        }

        public virtual void publishMessageValue()
        {
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => Value), InterfaceFormatDecimalPoint);
        }

        protected void Read(object? state)
        {
            MeasurementResult mr = BaseMeasurement.Measure();

            if (mr.ReturnCode == (int)MeasurmentResultCode.SUCCESS)
            {
                Logger.Information($"Measurment succesful: Value {Value} at {TimeStamp:yyyy-MM-dd HH:mm:ss-fff}, Status: {mr.StatusInfo}");
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