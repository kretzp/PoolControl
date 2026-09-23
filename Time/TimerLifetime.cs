using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PoolControl.Time;

// Owns every timer, including callbacks still running after an interval was disabled.
public sealed class TimerLifetime
{
    private readonly object _gate = new();
    private readonly HashSet<Timer> _timers = new();
    private readonly List<Task> _retired = new();
    private Task? _stopTask;

    public Timer? Restart(Timer? timer, TimerCallback callback, TimeSpan dueTime, TimeSpan period)
    {
        lock (_gate)
        {
            if (_stopTask != null) return null;
            _retired.RemoveAll(task => task.IsCompletedSuccessfully);
            if (period <= TimeSpan.Zero)
            {
                if (timer != null && _timers.Remove(timer)) _retired.Add(timer.DisposeAsync().AsTask());
                return null;
            }
            if (timer == null || !_timers.Contains(timer))
            {
                timer = new Timer(callback, null, dueTime, period);
                _timers.Add(timer);
            }
            else timer.Change(dueTime, period);
            return timer;
        }
    }

    public Task StopAsync()
    {
        lock (_gate)
        {
            return _stopTask ??= Task.WhenAll(_retired.Concat(_timers.Select(t => t.DisposeAsync().AsTask())));
        }
    }
}
