using System.Device.Gpio;
using System.Diagnostics;
using PoolControl.Hardware;
using PoolControl.ViewModels;

void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
MeasurementResult Measure(DistanceMeasurement sensor)
{
    var task = Task.Run(sensor.Measure);
    Assert(task.Wait(TimeSpan.FromSeconds(3)), "Measurement did not terminate within the test deadline.");
    return task.GetAwaiter().GetResult();
}

foreach (var level in new[] { 0, 1 })
{
    var gpio = new FakeGpio { Read = () => level };
    var model = new Distance();
    var sensor = new DistanceMeasurement(gpio) { ModelBase = model };
    var result = Measure(sensor);
    Assert(result.ReturnCode == 99 && result.StatusInfo!.Contains("50 ms"), "Stuck echo must report a timeout.");
    Assert(model.Value == 42 && model.Publications == 0, "Timeout must not publish a measurement.");
    Assert(gpio.Starts == 1 && gpio.Stops == 1, "Trigger must be reset.");
    Console.WriteLine($"PASS: echo stuck {(level == 0 ? "low" : "high")} terminates without publishing");

    gpio.Read = null;
    model.NumberOfMeasurements = 3;
    result = Measure(sensor);
    Assert(result.ReturnCode == 1 && double.IsFinite(result.Result) && result.Result > 0, "A subsequent valid pulse must succeed.");
    Assert(gpio.Starts == 4 && model.Publications == 1, "Average must use all three measurements and publish once.");
    Console.WriteLine("PASS: timeout releases guard and normal multi-sample measurement succeeds");
}

using (var entered = new ManualResetEventSlim())
using (var release = new ManualResetEventSlim())
{
    var gpio = new FakeGpio { Read = () => { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException("Test release timed out"); return 0; } };
    var sensor = new DistanceMeasurement(gpio) { ModelBase = new Distance() };
    var first = Task.Run(sensor.Measure);
    try
    {
        Assert(entered.Wait(TimeSpan.FromSeconds(3)), "First measurement did not reach GPIO.");
        var second = Measure(sensor);
        Assert(second.ReturnCode == (int)MeasurementResultCode.Pending && gpio.Starts == 1, "Overlapping call must not touch the sensor.");
        Console.WriteLine("PASS: overlapping measurement is skipped without extra GPIO access");
    }
    finally { release.Set(); }
    Assert(first.Wait(TimeSpan.FromSeconds(3)), "First measurement did not finish after release.");
}

{
    var gpio = new FakeGpio { Read = () => throw new InvalidOperationException("GPIO read failed") };
    var sensor = new DistanceMeasurement(gpio) { ModelBase = new Distance() };
    Assert(Measure(sensor).ReturnCode == 99, "GPIO exception must become an error result.");
    gpio.Read = null;
    Assert(Measure(sensor).ReturnCode == 1, "GPIO exception must release the measurement guard.");
    Console.WriteLine("PASS: GPIO exception does not prevent the next measurement");
}

foreach (var count in new[] { 0, -1 })
{
    var gpio = new FakeGpio();
    var sensor = new DistanceMeasurement(gpio) { ModelBase = new Distance { NumberOfMeasurements = count } };
    Assert(Measure(sensor).ReturnCode == 99 && gpio.Starts == 0, "Invalid sample count must fail before hardware access.");
    Console.WriteLine($"PASS: sample count {count} is rejected");
}
{
    var gpio = new FakeGpio();
    var model = new Distance();
    var sensor = new DistanceMeasurement(gpio) { ModelBase = model };
    sensor.RequestStop();
    Assert(Measure(sensor).ReturnCode == 99 && gpio.Starts == 0, "Stopped measurement must not access GPIO.");
    Console.WriteLine("PASS: shutdown prevents new measurements");
}
{
    var gpio = new FakeGpio();
    var model = new Distance();
    var sensor = new DistanceMeasurement(gpio) { ModelBase = model };
    gpio.Read = () => { sensor.RequestStop(); return 0; };
    Assert(Measure(sensor).ReturnCode == 99 && model.Publications == 0, "Shutdown must cancel echo polling without publishing.");
    Assert(gpio.Stops == 1, "Trigger was not reset before cancellation.");
    Console.WriteLine("PASS: shutdown cancels an active measurement");
}
{
    var gpio = new FakeGpio();
    var sensor = new DistanceMeasurement(gpio) { ModelBase = new Distance { OpenedPins = (17, 18) } };
    Assert(Measure(sensor).ReturnCode == 1, "Measurement on opened pins failed.");
    Assert(gpio.LastTrigger == 17 && gpio.LastEcho == 18, "Edited configuration must not redirect live GPIO access before restart.");
    Console.WriteLine("PASS: measurement keeps using opened pins until restart");
}
Console.WriteLine("11 checks passed.");

sealed class FakeGpio : IGpio
{
    public Func<int>? Read;
    public int Starts;
    public int Stops;
    public int LastTrigger;
    public int LastEcho;
    private Stopwatch? _pulse;
    public int ReadPin(int pin)
    {
        LastEcho = pin;
        if (Read != null) return Read();
        _pulse ??= Stopwatch.StartNew();
        return _pulse.ElapsedMilliseconds < 4 ? 1 : 0;
    }
    public void On(int pin, bool highIsOn) { LastTrigger = pin; Starts++; _pulse = null; }
    public void Off(int pin, bool highIsOn) => Stops++;
    public void Close(int pin) { }
    public void Close(int[] pins, bool highIsOn) { }
    public void DoSwitch(int pin, bool state, bool highIsOn) { }
    public void DoSwitch(int[] pins, bool state, bool highIsOn) { }
    public void Off(int[] pins, bool highIsOn) { }
    public void On(int[] pins, bool highIsOn) { }
    public void Open(int pin, PinMode mode, bool highIsOn) { }
    public void OpenPinModeInput(int pin, bool highIsOn) { }
    public void OpenPinModeOutput(int pin, bool highIsOn) { }
    public void OpenPinModeOutput(int[] pins, bool highIsOn) { }
    public void Dispose() { }
}
