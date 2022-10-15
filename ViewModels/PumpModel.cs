using Newtonsoft.Json;
using PoolControl.Time;
using System;
using System.Threading;

namespace PoolControl.ViewModels
{
    /// <summary>
    /// Base Data for pumps
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class PumpModel : ViewModelBase
    {
        protected TimeSpan DAILY = new TimeSpan(24, 0, 0);

        [JsonIgnore]
        public Switch Switch { get; set; }

        public abstract void OnTemperatureChange(MeasurementArgs args);

        public abstract void RecalculateThings();

        public void StartTimerTriggered()
        {
            Logger.Debug($"Schalte {GetType().Name} ein");

            if (WinterMode)
            {
                Logger.Information("WinterMode! Nothing to do!");
            }

            if (Switch != null)
            {
                Switch.On = true;
            }
            else
            {
                Logger.Debug($"{GetType().Name} is null and could not be turned on");
            }
            RecalculateThings();
        }

        public void EndTimerTriggered()
        {
            Logger.Debug($"Schalte {GetType().Name} aus");

            if (WinterMode)
            {
                Logger.Information("WinterMode! Nothing to do!");
            }

            if (Switch != null)
            {
                Switch.On = false;
            }
            else
            {
                Logger.Debug($"{GetType().Name} is null and could not be turned off");
            }
            RecalculateThings();
        }

        protected TimeTrigger InitializeTrigger(Action action, TimeSpan period, string name)
        {
            TimeTrigger trigger = new TimeTrigger();
            trigger.Name = name;
            trigger.Period = period;
            trigger.OnTimeTriggered += action;

            return trigger;
        }

        protected void startTrigger(TimeTrigger trigger, TimeSpan startTime)
        {
            trigger.StartTime = startTime;
            trigger.InitiateTimer();
        }

        protected string GetTimerName(string name)
        {
            return $"{GetType().Name}.{name}";
        }
    }
}
