using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Log = PoolControl.Helper.Log;

namespace PoolControl.Time;

public class TimeTrigger
{
    private readonly ILogger _logger;

    public string Name { get; init; } = "NoName";

    public TimeSpan StartTime { get; set; }

    public TimeSpan Period { get; init; }

    public DateTime TriggerTime { get; private set; }

    private Timer? _timer;
    private readonly TimerLifetime _lifetime = new();
    private readonly object _scheduleGate = new();
    private bool _stopped;

    public Task StopAsync()
    {
        lock (_scheduleGate)
        {
            _stopped = true;
            return _lifetime.StopAsync();
        }
    }


    public TimeTrigger()
    {
        _logger = Log.Logger?.ForContext<TimeTrigger>() ?? throw new ArgumentNullException(nameof(_logger));
    }

    private void OnTimerTicked(object? state)
    {
        OnTimeTriggered?.Invoke();
        InitiateTimer();
    }

    public void InitiateTimer()
    {
        lock (_scheduleGate)
        {
            if (_stopped) return;
            if(StartTime <= TimeSpan.Zero || Period <= TimeSpan.Zero)
            {
                _logger.Debug("Timer {Name} not started because of zero values StartTime: {StartTime} Period: {Period}", Name, StartTime, Period);
                return;
            }

            DateTime now = DateTime.Now;
            var triggerTime = DateTime.Today + StartTime - now;
            while(triggerTime < TimeSpan.Zero)
            {
                triggerTime += Period;
            }

            _timer = _lifetime.Restart(_timer, OnTimerTicked, triggerTime, Period);

            TriggerTime = now + triggerTime;
            _logger.Debug("Timer: {Name} TriggerTime: {TriggerTime} StartTime: {StartTime} Period: {Period}", Name, TriggerTime, StartTime, Period);
        }
    }

    public event Action? OnTimeTriggered;
}
