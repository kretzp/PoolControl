using System;
using System.Linq;
using System.Text;

namespace PoolControl.Communication;

public static class MqttCommandInput
{
    private const int MaxPathLength = 256;
    private const int MaxSegmentLength = 64;
    private const int MaxPayloadBytes = 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static bool TrySplitPath(string path, out string[] segments, out string error)
    {
        segments = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(path) || path.Length > MaxPathLength)
        {
            error = "The MQTT command path is empty or too long.";
            return false;
        }

        if (path.IndexOfAny(new[] { '#', '+', '\0' }) >= 0)
        {
            error = "The MQTT command path contains invalid characters.";
            return false;
        }

        segments = path.Split('/');
        if (segments.Length is < 1 or > 3 || segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) || segment.Length > MaxSegmentLength))
        {
            segments = Array.Empty<string>();
            error = "The MQTT command path must contain one to three non-empty segments.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryDecodePayload(ReadOnlySpan<byte> payload, out string value, out string error)
    {
        value = string.Empty;
        if (payload.Length is 0 or > MaxPayloadBytes)
        {
            error = "The MQTT payload is empty or too large.";
            return false;
        }

        try
        {
            value = StrictUtf8.GetString(payload);
        }
        catch (DecoderFallbackException)
        {
            error = "The MQTT payload is not valid UTF-8.";
            return false;
        }

        if (value.IndexOf('\0') >= 0)
        {
            value = string.Empty;
            error = "The MQTT payload contains a null character.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
