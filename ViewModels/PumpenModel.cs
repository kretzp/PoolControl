﻿using Newtonsoft.Json;
using PoolControl.Time;
using System;
using System.Threading;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class PumpenModel : ViewModelBase
    {
        protected TimeSpan DAILY = new TimeSpan(24, 0, 0);

        [JsonIgnore]
        public Switch Switch { get; set; }

        public abstract void OnTemperatureChange(MeasurementArgs args);

        public abstract void RecalculateThings();

        public void StartTimerTriggered()
        {
            Logger.Debug($"Schalte {GetType().Name} ein");
            if (Switch != null)
            {
                Switch.On = true;
            }
            else
            {
                Logger.Debug($"{GetType().Name} ist null und konnte nicht eingeschaltet werden");
            }
            RecalculateThings();
        }

        public void EndeTimerTriggered()
        {
            Logger.Debug($"Schalte {GetType().Name} aus");
            if (Switch != null)
            {
                Switch.On = false;
            }
            else
            {
                Logger.Debug($"{GetType().Name} ist null und konnte nicht ausgeschaltet werden");
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
