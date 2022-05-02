using Microsoft.Extensions.Configuration;

namespace PoolControl.Helper
{
    public class PoolControlConfig
    {
        private static PoolControlConfig? _instance;
        private static readonly object padlock = new object();

        public static PoolControlConfig Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new PoolControlConfig();
                    }
                    return _instance;
                }
            }
        }

        public IConfiguration Config { get; private set; }
        public Settings Settings { get; private set; }

        private PoolControlConfig()
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            Settings = Config.GetRequiredSection("Settings").Get<Settings>();
        }
    }

    public class BaseTopicSettings
    {
        public string Command { get; set; } = "basetopic/cmd/";
        public string State { get; set; } = "basetopic/state/";
    }

    public class LWTSettings
    {
        public string ConnectMessage { get; set; } = "Connected";
        public string DisconnectMessage { get; set; } = "Connection Lost";
        public string Topic { get; set; } = "basetopic/LWT";
    }

    public class MQTTSettings
    {
        public string Password { get; set; } = "";
        public int Port { get; set; } = 1883;
        public string Server { get; set; } = "";

        public string User { get; set; } = "";
    }

    public class Settings
    {
        public BaseTopicSettings BaseTopic { get; set; } = null!;
        public LWTSettings LWT { get; set; } = null!;
        public MQTTSettings MQTT { get; set; } = null!;

        public string PersistenceFile { get; set; } = "poolcontrolviewmodel.json";
    }
}
