using System.Collections.Generic;
using Newtonsoft.Json;

namespace PoolControl.ViewModels;

/// <summary>
/// Configuration for relay to pump attachment
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class RelayConfig
{
    private static RelayConfig? _instance;
    private static readonly object Padlock = new object();

    public static RelayConfig? Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new RelayConfig();
            }
        }

        set => _instance = value;
    }

    private RelayConfig() { }

    [JsonProperty("RelayToLogicLevelConverter")]
    public Dictionary<int, int>? RelayToLogicLevelConverterDict;

    [JsonProperty("LogicLevelConverterToGpio")]
    public Dictionary<int, int>? LogicLevelConverterToGpioDict;

    public int GetGpioForRelayNumber(int relayNumber)
    {
        if (RelayToLogicLevelConverterDict != null && RelayToLogicLevelConverterDict.TryGetValue(relayNumber, out var key))
        {
            if (LogicLevelConverterToGpioDict != null && LogicLevelConverterToGpioDict.TryGetValue(key, out var number))
            {
                return number;
            }
        }

        return -1;
    }
}