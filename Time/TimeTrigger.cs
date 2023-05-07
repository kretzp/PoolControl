using Serilog;
using System;
using System.Threading;
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

        if (_timer == null)
        {
            var autoEvent = new AutoResetEvent(false);
            _timer = new Timer(OnTimerTicked, autoEvent, triggerTime, Period);
            _logger.Debug("New Timer {Name} created", Name);
        }
        else
        {
            _timer.Change(triggerTime, Period);
        }

        TriggerTime = now + triggerTime;
        _logger.Debug("Timer: {Name} TriggerTime: {TriggerTime} StartTime: {StartTime} Period: {Period}", Name, TriggerTime, StartTime, Period);
    }

    public event Action? OnTimeTriggered;
}