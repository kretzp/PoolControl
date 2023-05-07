using MQTTnet;
using MQTTnet.Client;
using Serilog;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using PoolControl.Helper;
using Log = PoolControl.Helper.Log;

namespace PoolControl.Communication;

public class PoolMqttClient
{
    private const string Reason = "SHUTDOWN";
    private const string Win = "win";

    private bool Shutdown { get; set; }

    private static PoolMqttClient? _instance;
    private static readonly object Padlock = new object();

    public static PoolMqttClient Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new PoolMqttClient(Log.Logger);
            }
        }
    }

    private MqttFactory? _mqttFactory;
    private IMqttClient? _mqttClient;
    private MqttClientOptions? _options;
    protected ILogger Logger { get; init; }

    private PoolMqttClient(ILogger? logger)
    {
        Logger = logger?.ForContext<PoolMqttClient>() ?? throw new ArgumentNullException(nameof(Logger));
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Logger.Information("# Start MQTT #");
        _mqttFactory = new MqttFactory();
        _mqttClient = _mqttFactory.CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(PoolControlConfig.Instance.Settings!.MQTT.Server, PoolControlConfig.Instance.Settings.MQTT.Port)
            .WithCredentials(PoolControlConfig.Instance.Settings.MQTT.User, PoolControlConfig.Instance.Settings.MQTT.Password)
            .Build();

        _mqttClient.DisconnectedAsync += async (e) =>
        {
            Logger.Information("# DISCONNECTED FROM SERVER #");
            _ = Task.Delay(TimeSpan.FromSeconds(5));
            if (!Reason.Equals(e.ReasonString))
            {
                if (Shutdown)
                {
                    Logger.Information("## SHUTDOWN in Progress while connecting Async. NO CONNECTION will be restarted ##");
                }
                else
                {
                    await connectAsync();
                }
            }
            else
            {
                Logger.Information("# NO RECONNECTION BECAUSE OF INTENTIONALLY DISCONNECTION #");
            }
        };

        _mqttClient.ConnectedAsync += async (_) =>
        {
            Logger.Information("# CONNECTED WITH SERVER #");
            await sendLwtConnected();

            // Subscribe to a topic
            string topic = $"{PoolControlConfig.Instance.Settings.BaseTopic.Command}#";

            subscribe(topic);
        };

        _mqttClient.ApplicationMessageReceivedAsync += (e) => {
            Logger.Information("# Received Topic={Topic} Payload={Payload} QoS={Qos} Retain={Retain}",
                e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()), e.ApplicationMessage.QualityOfServiceLevel, e.ApplicationMessage.Retain);
            return Task.CompletedTask;
        };

        await connectAsync();
    }

    private async Task sendLwtConnected()
    {
        string lwtTopic = PoolControlConfig.Instance.Settings!.LWT.Topic;
        string? lwtConnectMessage = PoolControlConfig.Instance.Settings.LWT.ConnectMessage;
        await publishMessage(lwtTopic, lwtConnectMessage, 2, true);
    }

    private async Task sendLwtDisconnected()
    {
        string lwtTopic = PoolControlConfig.Instance.Settings!.LWT.Topic;
        string? lwtDisConnectMessage = PoolControlConfig.Instance.Settings.LWT.DisconnectMessage;
        await publishMessage(lwtTopic, lwtDisConnectMessage, 2, true);
        Logger.Information("Topic: {Topic} Payload: {Payload}", lwtTopic, lwtDisConnectMessage);
    }

    public void register(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
    {
        if (_mqttClient != null) _mqttClient.ApplicationMessageReceivedAsync += handler;
    }

    public void unRegister(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
    {
        if (_mqttClient != null) _mqttClient.ApplicationMessageReceivedAsync -= handler;
    }

    private async void subscribe(string topic)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            topic = Win + topic;
        }

        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
        Logger.Information("# Subscribed topic={Topic}", topic);
    }

    public async void unSubscribe(string topic)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            topic = Win + topic;
        }

        await _mqttClient.UnsubscribeAsync(topic);
        Logger.Information("# Unsubscribed topic={Topic}", topic);
    }

    private async Task connectAsync()
    {
        try
        {
            Logger.Information("# CONNECTING ... #");
            if (_mqttClient != null) await _mqttClient.ConnectAsync(_options);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "# RECONNECTING FAILED ##");
        }
    }

    public void Disconnect()
    {
        try
        {
            _ = sendLwtDisconnected();
            Thread.Sleep(3000);
            Shutdown = true;
            if (_mqttClient != null)
                _ = _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder()
                    .WithReason(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage).Build());
            Logger.Information("# Disconnect started because of shutdown");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "# DISCONNECTING FAILED #");
        }
    }

    public async Task publishMessage(string topic, string? payload, int qos, bool retain)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            topic = Win + topic;
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
            .WithRetainFlag(retain)
            .Build();

        if (_mqttClient != null) await _mqttClient.PublishAsync(message);
        Logger.Information("# Published  Topic={Topic} Payload={Payload} QoS={Qos} Retain={Retain}", message.Topic, Encoding.UTF8.GetString(message.PayloadSegment.ToArray()), message.QualityOfServiceLevel, message.Retain);

        Process currentProc = Process.GetCurrentProcess();
        double bytesInUse = currentProc.PrivateMemorySize64 / 1024.0 / 1024.0;

        Logger.Debug("MBytes in use {Bytes} in {Proc}", bytesInUse, currentProc.ProcessName);
    }
}