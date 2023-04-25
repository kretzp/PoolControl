using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using Serilog;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoolControl.Helper;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PoolControl.Communication
{
    public class PoolMqttClient
    {
        private const string REASON = "SHUTDOWN";
        private const string WIN = "win";

        public bool Shutdown { get; set; }

        private static PoolMqttClient? _instance;
        private static readonly object padlock = new object();

        public static PoolMqttClient Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new PoolMqttClient(Log.Logger);
                    }
                    return _instance;
                }
            }
        }

        private MqttFactory _mqttFactory;
        private MQTTnet.Client.IMqttClient _mqttClient;
        private MqttClientOptions options;
        protected ILogger Logger { get; set; }

        public PoolMqttClient(ILogger logger)
        {
            Logger = logger?.ForContext<PoolMqttClient>() ?? throw new ArgumentNullException(nameof(Logger));
            _ = InitlializeAsync();
        }

        public async Task InitlializeAsync()
        {
            Logger.Information("# Start MQTT #");
            _mqttFactory = new MqttFactory();
            _mqttClient = _mqttFactory.CreateMqttClient();
            options = new MqttClientOptionsBuilder()
                            .WithTcpServer(PoolControlConfig.Instance.Settings.MQTT.Server, PoolControlConfig.Instance.Settings.MQTT.Port)
                            .WithCredentials(PoolControlConfig.Instance.Settings.MQTT.User, PoolControlConfig.Instance.Settings.MQTT.Password)
                            .Build();

            _mqttClient.DisconnectedAsync += async (e) =>
            {
                Logger.Information("# DISCONNECTED FROM SERVER #");
                _ = Task.Delay(TimeSpan.FromSeconds(5));
                if (!REASON.Equals(e.ReasonString))
                {
                    if (Shutdown)
                    {
                        Logger.Information("## SHUTDOWN in Progess while connecting Async. NO CONNECTION will be restarted ##");
                    }
                    else
                    {
                        await connectAsync();
                    }
                }
                else
                {
                    Logger.Information("# NO RECONNECTION BECAUSE OF INTENTIONALLY DISONNETION #");
                }
            };

            _mqttClient.ConnectedAsync += async (e) =>
            {
                Logger.Information("# CONNECTED WITH SERVER #");
                await SendLWTConnected();

                // Subscribe to a topic
                string topic = $"{PoolControlConfig.Instance.Settings.BaseTopic.Command}#";

                subscribe(topic);
            };

            _mqttClient.ApplicationMessageReceivedAsync += async (e) =>
            {
                Logger.Information($"# Recived Topic={e.ApplicationMessage.Topic} Payload={Encoding.UTF8.GetString(e.ApplicationMessage.Payload)} QoS={e.ApplicationMessage.QualityOfServiceLevel} Retain={e.ApplicationMessage.Retain}");
            };

            await connectAsync();
        }

        private async Task SendLWTConnected()
        {
            string lwtTopic = PoolControlConfig.Instance.Settings.LWT.Topic;
            string lwtConnectMessage = PoolControlConfig.Instance.Settings.LWT.ConnectMessage;
            await publishMessage(lwtTopic, lwtConnectMessage, 2, true);
        }

        public async Task SendLWTDisconnected()
        {
            string lwtTopic = PoolControlConfig.Instance.Settings.LWT.Topic;
            string lwtDisConnectMessage = PoolControlConfig.Instance.Settings.LWT.DisconnectMessage;
            await publishMessage(lwtTopic, lwtDisConnectMessage, 2, true);
            Logger.Information($"Topic: {lwtTopic} Payload: {lwtDisConnectMessage}");
        }

        public void register(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
        {
            _mqttClient.ApplicationMessageReceivedAsync += handler;
        }

        public void unRegister(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
        {
            _mqttClient.ApplicationMessageReceivedAsync -= handler;
        }

        public async void subscribe(string topic)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                topic = WIN + topic;
            }

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
            Logger.Information($"# Subscribed topic={topic}");
        }

        public async void unSubscribe(string topic)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                topic = WIN + topic;
            }

            await _mqttClient.UnsubscribeAsync(topic);
            Logger.Information($"# Unsubscribed topic={topic}");
        }

        private async Task connectAsync()
        {
            try
            {
                Logger.Information("# CONNECTING ... #");
                await _mqttClient.ConnectAsync(options);
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
                _ = SendLWTDisconnected();
                Thread.Sleep(3000);
                Shutdown = true;
                _ = _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage).Build());
                Logger.Information("# Disconnect started because of shutdown");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "# DISCONNECTING FAILED #");
            }
        }

        public async Task publishMessage(string topic, string payload, int qos, bool retain)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                topic = WIN + topic;
            }

                var message = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                            .WithRetainFlag(retain)
                            .Build();

            await _mqttClient.PublishAsync(message);
            Logger.Information($"# Published  Topic={message.Topic} Payload={Encoding.UTF8.GetString(message.Payload)} QoS={message.QualityOfServiceLevel} Retain={message.Retain}");

            Process currentProc = Process.GetCurrentProcess();
            double bytesInUse = currentProc.PrivateMemorySize64 / 1024 / 1024;

            Logger.Debug("MBytes in use {bytes} in {proc}", bytesInUse, currentProc.ProcessName);
        }
    }
}
