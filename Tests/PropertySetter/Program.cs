using PoolControl.Helper;
using System.ComponentModel.DataAnnotations;

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var root = new Root();
var result = PropertySetter.setProperty(root, nameof(Root.WinterMode), "true");
Assert(result.Success && root.WinterMode, "Direct property was not set.");
Console.WriteLine("PASS: direct root property");

result = PropertySetter.setProperty(root, nameof(Settings.Limit), "7.25", nameof(Root.Settings));
Assert(result.Success && root.Settings.Limit == 7.25, "Nested property was not set.");
Console.WriteLine("PASS: nested property");

result = PropertySetter.setProperty(root, nameof(Device.Enabled), "off", nameof(Root.Devices), "pump");
Assert(result.Success && !((Device)root.DevicesObj["pump"]).Enabled, "Dictionary property was not set.");
Console.WriteLine("PASS: dictionary property");

result = PropertySetter.setProperty(root, "Missing", "1");
Assert(!result.Success, "Unknown root property was accepted.");
Console.WriteLine("PASS: unknown root property rejected");

result = PropertySetter.setProperty(root, nameof(Root.Count), "not-a-number");
Assert(!result.Success && root.Count == 0, "Invalid value changed the property.");
Console.WriteLine("PASS: invalid root value rejected");

result = PropertySetter.setProperty(root, nameof(Root.IntervalInSec), "0");
Assert(!result.Success && root.IntervalInSec == 60, "Out-of-range interval was applied.");
result = PropertySetter.setProperty(root, nameof(Root.IntervalInSec), "120");
Assert(result.Success && root.IntervalInSec == 120, "Valid interval was rejected.");
Console.WriteLine("PASS: annotated time interval is validated before assignment");

result = PropertySetter.setProperty(root, nameof(Root.Limit), "NaN");
Assert(!result.Success && root.Limit == 1, "Non-finite numeric input was applied.");
Console.WriteLine("PASS: non-finite MQTT numeric input rejected");

result = PropertySetter.setProperty(root, nameof(Root.WinterMode), "perhaps");
Assert(!result.Success && root.WinterMode, "Ambiguous boolean input was applied.");
Console.WriteLine("PASS: ambiguous MQTT boolean input rejected");

Console.WriteLine("8 property setter checks passed.");

public sealed class Root
{
    public bool WinterMode { get; set; }
    public int Count { get; set; }
    [Range(1, 86400)] public int IntervalInSec { get; set; } = 60;
    public double Limit { get; set; } = 1;
    public Settings Settings { get; } = new();
    public Dictionary<string, object> DevicesObj { get; } = new() { ["pump"] = new Device { Enabled = true } };
    public object Devices => DevicesObj;
}

public sealed class Settings
{
    public double Limit { get; set; }
}

public sealed class Device
{
    public bool Enabled { get; set; }
}
