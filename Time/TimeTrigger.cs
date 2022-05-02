using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoolControl.Time
{
    public class TimeTrigger
    {
        private ILogger Logger;

        private TimeSpan startTime;

        public string Name { get; set; } = "NoName";

        public TimeSpan StartTime { get; set; }

        private TimeSpan period;
        public TimeSpan Period
        {
            get => period;
            set
            {
                period = value;
            }
        }

        public DateTime TriggerTime { get; protected set; }

        private Timer timer;


        public TimeTrigger()
        {
            Logger = Log.Logger?.ForContext<TimeTrigger>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        private void OnTimerTicked(object? state)
        {
            OnTimeTriggered?.Invoke();
            InitiateTimer();
        }

        public void InitiateTimer()
        {
            if(StartTime <= TimeSpan.Zero || Period <= TimeSpan.Zero)
            {
                Logger.Debug($"Timer {Name} not started because of zero values StartTime: {StartTime} Period: {Period}");
                return;
            }

            DateTime now = DateTime.Now;
            var triggerTime = DateTime.Today + StartTime - now;
            while(triggerTime < TimeSpan.Zero)
            {
                triggerTime += Period;
            }

            if (timer == null)
            {
                var autoEvent = new AutoResetEvent(false);
                timer = new Timer(OnTimerTicked, autoEvent, triggerTime, Period);
                Logger.Debug($"New Timer {Name} created");
            }
            else
            {
                timer.Change(triggerTime, Period);
            }

            TriggerTime = now + triggerTime;
            Logger.Debug($"Timer: {Name} TriggerTime: {TriggerTime} StartTime: {StartTime} Period: {Period}");
        }

        public event Action OnTimeTriggered;
    }
}
