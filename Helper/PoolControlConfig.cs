using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace PoolControl.Helper;

public class PoolControlConfig
{
    private static PoolControlConfig? _instance;
    private static readonly object Padlock = new();

    public static PoolControlConfig Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new PoolControlConfig();
            }

        }
    }

    private IConfiguration? Config { get; set; }
    public Settings? Settings { get; private set; }

    private PoolControlConfig()
    {
        Config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Settings = Config.GetRequiredSection("Settings").Get<Settings>();
    }
}

[UsedImplicitly]
public class BaseTopicSettings
{
    public string Command { get; set; } = "basetopic/cmd/";
    public string State { get; set; } = "basetopic/state/";
}

[UsedImplicitly]
public class LWTSettings
{
    public string? ConnectMessage { get; set; } = "Connected";
    public string? DisconnectMessage { get; set; } = "Connection Lost";
    public string Topic { get; set; } = "basetopic/LWT";
}

[UsedImplicitly]
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

    public int PersistenceSaveIntervalInSec { get; set; } = 60;
}