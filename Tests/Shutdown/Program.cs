using PoolControl.Time;

void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
async Task Complete(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(5));
var period = TimeSpan.FromSeconds(10);

// All managed timers (measurement and the two pH timers) must drain together.
{
    var owner = new TimerLifetime();
    using var entered = new CountdownEvent(3);
    using var release = new ManualResetEventSlim();
    bool closed = false;
    int lateAccess = 0;
    for (int i = 0; i < 3; i++)
        owner.Restart(null, _ => { entered.Signal(); release.Wait(TimeSpan.FromSeconds(5)); if (closed) Interlocked.Increment(ref lateAccess); }, TimeSpan.Zero, period);
    Assert(entered.Wait(TimeSpan.FromSeconds(5)), "Callbacks did not start");
    var stopped = owner.StopAsync();
    try
    {
        Assert(!stopped.IsCompleted, "Stop must wait for all active callbacks");
        Assert(ReferenceEquals(stopped, owner.StopAsync()), "Repeated stop must await the same work");
    }
    finally { release.Set(); }
    await Complete(stopped);
    closed = true;
    Assert(lateAccess == 0, "Hardware was closed before callbacks completed");
    Assert(owner.Restart(null, _ => throw new Exception("Late callback"), TimeSpan.Zero, period) == null, "Stopped owner allowed a new timer");
    Console.WriteLine("PASS: all active timers drain before hardware release; stop is idempotent and blocks restart");
}

// Disabling a timer while it is running must not lose track of its callback.
{
    var owner = new TimerLifetime();
    using var entered = new ManualResetEventSlim();
    using var release = new ManualResetEventSlim();
    var timer = owner.Restart(null, _ => { entered.Set(); release.Wait(TimeSpan.FromSeconds(5)); }, TimeSpan.Zero, period);
    Assert(entered.Wait(TimeSpan.FromSeconds(5)), "Callback did not start");
    owner.Restart(timer, _ => { }, TimeSpan.Zero, TimeSpan.Zero);
    var stopped = owner.StopAsync();
    try { Assert(!stopped.IsCompleted, "Disabled timer callback was not awaited"); }
    finally { release.Set(); }
    await Complete(stopped);
    Console.WriteLine("PASS: previously disabled timer callbacks are also awaited");
}

// A scheduled callback must be removed, not allowed to run after shutdown.
{
    var owner = new TimerLifetime();
    int calls = 0;
    owner.Restart(null, _ => Interlocked.Increment(ref calls), TimeSpan.FromMilliseconds(100), period);
    await Complete(owner.StopAsync());
    await Task.Delay(150);
    Assert(calls == 0, "Scheduled callback ran after stop");
    Console.WriteLine("PASS: pending timers are cancelled");
}

// A daily trigger rearms itself after its callback; stopping must prevent this.
{
    var trigger = new TimeTrigger { Name = "test", StartTime = DateTime.Now.TimeOfDay + TimeSpan.FromMilliseconds(50), Period = TimeSpan.FromMilliseconds(100) };
    using var entered = new ManualResetEventSlim();
    using var release = new ManualResetEventSlim();
    int calls = 0;
    trigger.OnTimeTriggered += () => { Interlocked.Increment(ref calls); entered.Set(); release.Wait(TimeSpan.FromSeconds(5)); };
    trigger.InitiateTimer();
    Assert(entered.Wait(TimeSpan.FromSeconds(5)), "Trigger did not fire");
    var stopped = trigger.StopAsync();
    try { Assert(!stopped.IsCompleted, "Trigger stop failed to drain the active callback"); }
    finally { release.Set(); }
    await Complete(stopped);
    int completedCalls = calls;
    trigger.InitiateTimer();
    await Task.Delay(200);
    Assert(calls == completedCalls, "Trigger rearmed itself after shutdown");
    Console.WriteLine("PASS: active time trigger drains and cannot rearm");
}
Console.WriteLine("All shutdown regression checks passed.");

namespace PoolControl.Helper
{
    public static class Log { public static Serilog.ILogger Logger { get; } = new Serilog.LoggerConfiguration().CreateLogger(); }
}
