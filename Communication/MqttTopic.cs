using System;
using System.Runtime.InteropServices;

namespace PoolControl.Communication;

public static class MqttTopic
{
    private const string WindowsPrefix = "win";

    public static string ForCurrentPlatform(string topic)
    {
        return ForPlatform(topic, RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }

    public static bool TryGetCommandPath(string receivedTopic, string commandTopic, out string commandPath)
    {
        return TryGetCommandPath(receivedTopic, commandTopic,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows), out commandPath);
    }

    internal static string ForPlatform(string topic, bool isWindows)
    {
        ArgumentNullException.ThrowIfNull(topic);
        return isWindows ? WindowsPrefix + topic : topic;
    }

    internal static bool TryGetCommandPath(
        string receivedTopic,
        string commandTopic,
        bool isWindows,
        out string commandPath)
    {
        ArgumentNullException.ThrowIfNull(receivedTopic);
        ArgumentException.ThrowIfNullOrEmpty(commandTopic);

        var expectedPrefix = ForPlatform(commandTopic, isWindows);
        if (!receivedTopic.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            commandPath = string.Empty;
            return false;
        }

        commandPath = receivedTopic[expectedPrefix.Length..];
        return commandPath.Length > 0;
    }
}
