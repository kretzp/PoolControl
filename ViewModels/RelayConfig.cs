using System.Collections.Generic;
using Newtonsoft.Json;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RelayConfig
    {
        private static RelayConfig? _instance;
        private static readonly object padlock = new object();

        public static RelayConfig Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new RelayConfig();
                    }
                    return _instance;
                }
            }

            set
            {
                _instance = value;
            }
        }

        private RelayConfig() { }

        [JsonProperty("RelayToLogicLevelConverter")]
        public Dictionary<int, int>? RelayToLogicLevelConverterDict;

        [JsonProperty("LogicLevelConverterToGpio")]
        public Dictionary<int, int>? LogicLevelConverterToGpioDict;

        public int GetGpioForRelayNumber(int relayNumber)
        {
            if (RelayToLogicLevelConverterDict != null && RelayToLogicLevelConverterDict.ContainsKey(relayNumber))
            {
                int key = RelayToLogicLevelConverterDict[relayNumber];
                if (LogicLevelConverterToGpioDict != null && LogicLevelConverterToGpioDict.ContainsKey(key))
                {
                    return LogicLevelConverterToGpioDict[key];
                }
            }

            return -1;
        }
    }
}
