using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using Newtonsoft.Json;
using PoolControl.Time;
using System.Threading;
using System.Reactive;
using PoolControl.Hardware;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// Base class for pH measurment
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Ph : EzoBase
    {
        public Ph()
        {
            Logger = Log.Logger?.ForContext<Ph>() ?? throw new ArgumentNullException(nameof(Logger));

            OnMidCal = ReactiveCommand.Create(MidCal_Button_Clicked);
            OnLowCal = ReactiveCommand.Create(LowCal_Button_Clicked);
            OnHighCal = ReactiveCommand.Create(HighCal_Button_Clicked);
            OnGetSlope = ReactiveCommand.Create(GetSlope_Button_Clicked);

            // Publish changes via MQTT
            this.WhenAnyValue(p => p.MaxValue).Subscribe((Action<double>)(value => publishMessageWithType(PoolControlHelper.GetPropertyName(() => MaxValue), PoolControlHelper.format1Decimal(value))));
            this.WhenAnyValue(p => p.AcidInjectionDuration).Subscribe(einlaufdauerinterval => RestartPhTimerAndPublishNewInterval());
            this.WhenAnyValue(p => p.AcidInjectionRecurringPeriod).Subscribe(einlaufdauerinterval => RestartPhTimerAndPublishNewInterval());
        }

        private void GetSlope_Button_Clicked()
        {
            Slope = ((PhMeasurement)BaseMeasurement).slope().StatusInfo;
        }

        private void MidCal_Button_Clicked()
        {
            ((PhMeasurement)BaseMeasurement).midCalibration(MidCal);
        }

        private void LowCal_Button_Clicked()
        {
            ((PhMeasurement)BaseMeasurement).lowCalibration(LowCal);
        }

        private void HighCal_Button_Clicked()
        {
            ((PhMeasurement)BaseMeasurement).highCalibration(HighCal);
        }

        public void RestartPhTimerAndPublishNewInterval()
        {
            PhTimerOn = RestartTimer(PhTimerOn, CheckPh, PhInterval);
            PhTimerOff = RestartTimer(PhTimerOff, PhPumpOff, PhInterval + 1000 * AcidInjectionDuration, PhInterval);

            publishMessageWithType(PoolControlHelper.GetPropertyName(() => AcidInjectionDuration), AcidInjectionDuration.ToString());
            publishMessageWithType(PoolControlHelper.GetPropertyName(() => AcidInjectionRecurringPeriod), AcidInjectionRecurringPeriod.ToString());
        }

        private void PhPumpOff(object? state)
        {
            if(Switch != null)
            {
                Switch.On = false;
                Logger.Information($"{Switch.Name} Ein({Switch.On})");
            }
        }

        protected void CheckPh(object? state)
        {
            if (Switch != null)
            {
                if (FilterPumpSwitch.On)
                {
                    if (Value > MaxValue)
                    {
                        Switch.On = true;
                        Logger.Information($"{Switch.Name} Ein({Switch.On}) pH({Value}) > MaxPh({MaxValue}) FilterPump({FilterPumpSwitch.On})");
                    }
                    else
                    {
                        Logger.Information($"{Switch.Name} Ein({Switch.On}) pH({Value}) < MaxPh({MaxValue}) FilterPump({FilterPumpSwitch.On})");
                    }
                }
                else
                {
                    if (Switch.On)
                    {
                        Switch.On = false;
                        Logger.Information($"Ein({Switch.On}) FilterPump({FilterPumpSwitch.On})");
                    }
                }
            }
        }

        public ReactiveCommand<Unit, Unit> OnMidCal { get; }
        public ReactiveCommand<Unit, Unit> OnLowCal { get; }
        public ReactiveCommand<Unit, Unit> OnHighCal { get; }
        public ReactiveCommand<Unit, Unit> OnGetSlope { get; }

        [JsonIgnore]
        protected Timer? PhTimerOn { get; private set; }

        [JsonIgnore]
        protected Timer? PhTimerOff { get; private set; }

        [JsonIgnore]
        public Temperature PoolTemperature { get; set; }

        [Reactive]
        [JsonProperty]
        public double MidCal { get; set; }

        [Reactive]
        [JsonProperty]
        public double LowCal { get; set; }

        [Reactive]
        [JsonProperty]
        public double HighCal { get; set; }

        [Reactive]
        [JsonProperty]
        public double MaxValue { get; set; }

        [Reactive]
        [JsonProperty]
        public int AcidInjectionDuration { get; set; }

        [Reactive]
        [JsonProperty]
        public int AcidInjectionRecurringPeriod { get; set; }


        [Reactive]
        [JsonProperty]
        public string Slope { get; set; }

        [JsonIgnore]
        public int PhInterval { get { return AcidInjectionRecurringPeriod * 60 * 1000; } }

        public override void OnValueChange()
        {
            // Nothing to do
        }
    }
}
