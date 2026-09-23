using PoolControl.Communication;

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

const string commandTopic = "PoolControl/cmd/";

Assert(MqttTopic.ForPlatform(commandTopic + "#", false) == "PoolControl/cmd/#",
    "Linux topic was changed.");
Assert(MqttTopic.ForPlatform(commandTopic + "#", true) == "winPoolControl/cmd/#",
    "Windows prefix was not applied.");
Console.WriteLine("PASS: outgoing topics use the platform prefix exactly where required");

Assert(MqttTopic.TryGetCommandPath("PoolControl/cmd/sendstate", commandTopic, false, out var linuxPath)
       && linuxPath == "sendstate", "Linux command path was not extracted.");
Assert(MqttTopic.TryGetCommandPath("winPoolControl/cmd/sendstate", commandTopic, true, out var windowsPath)
       && windowsPath == "sendstate", "Windows command path retained the win prefix.");
Console.WriteLine("PASS: Linux and Windows command paths are normalized identically");

Assert(MqttTopic.TryGetCommandPath("winPoolControl/cmd/Temperatures/Pool/IntervalInSec",
        commandTopic, true, out var nestedPath)
       && nestedPath == "Temperatures/Pool/IntervalInSec", "Nested Windows path was damaged.");
Console.WriteLine("PASS: nested Windows command path is preserved");

Assert(!MqttTopic.TryGetCommandPath("winPoolControl/state/sendstate", commandTopic, true, out _),
    "State topic was accepted as a command.");
Assert(!MqttTopic.TryGetCommandPath("prefix/PoolControl/cmd/sendstate", commandTopic, false, out _),
    "Embedded command prefix was accepted.");
Assert(!MqttTopic.TryGetCommandPath("PoolControl/cmd/", commandTopic, false, out _),
    "Empty command path was accepted.");
Assert(!MqttTopic.TryGetCommandPath("PoolControl/cmd/sendstate", commandTopic, true, out _),
    "Unprefixed command was accepted on Windows.");
Console.WriteLine("PASS: unrelated, embedded, empty and wrong-platform topics are rejected");

Assert(MqttCommandInput.TrySplitPath("Temperatures/Pool/IntervalInSec", out var commandSegments, out _)
       && commandSegments.SequenceEqual(new[] { "Temperatures", "Pool", "IntervalInSec" }),
    "Valid property command path was rejected.");
Assert(!MqttCommandInput.TrySplitPath("Temperatures//IntervalInSec", out _, out _)
       && !MqttCommandInput.TrySplitPath("a/b/c/d", out _, out _),
    "Malformed command path was accepted.");
Console.WriteLine("PASS: MQTT command paths are structurally validated");

Assert(MqttCommandInput.TryDecodePayload("120"u8, out var payload, out _) && payload == "120",
    "Valid UTF-8 payload was rejected.");
Assert(!MqttCommandInput.TryDecodePayload(new byte[] { 0xC3, 0x28 }, out _, out _),
    "Invalid UTF-8 payload was accepted.");
Assert(!MqttCommandInput.TryDecodePayload(new byte[1025], out _, out _),
    "Oversized payload was accepted.");
Console.WriteLine("PASS: MQTT payload size and UTF-8 are validated");

Console.WriteLine("6 MQTT topic and input checks passed.");
