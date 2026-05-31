using MQTTnet;
using MQTTnet.Client;
using Serilog;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoolControl.Helper;
using Log = PoolControl.Helper.Log;

namespace PoolControl.Communication;

public class PoolMqttClient : IPoolMqttClient
{
    private const string Reason = "SHUTDOWN";
    private const string Win = "win";

    private static PoolMqttClient? _instance;
    private static readonly object Padlock = new object();

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly TaskCompletionSource<bool> _initializationTask = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private MqttFactory? _mqttFactory;
    private IMqttClient? _mqttClient;
    private MqttClientOptions? _options;
    private bool _shutdown;
    private bool _disposed;

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

    public Task InitializationTask => _initializationTask.Task;

    protected ILogger Logger { get; init; }

    public PoolMqttClient(ILogger? logger = null)
    {
        Logger = logger?.ForContext<PoolMqttClient>() ?? Log.Logger?.ForContext<PoolMqttClient>() ?? throw new ArgumentNullException(nameof(logger));
        _ = InitializeAsync(_cancellationTokenSource.Token);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.Information("# Start MQTT #");
            _mqttFactory = new MqttFactory();
            _mqttClient = _mqttFactory.CreateMqttClient();
            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(PoolControlConfig.Instance.Settings!.MQTT.Server, PoolControlConfig.Instance.Settings.MQTT.Port)
                .WithCredentials(PoolControlConfig.Instance.Settings.MQTT.User, PoolControlConfig.Instance.Settings.MQTT.Password)
                .Build();

            _mqttClient.DisconnectedAsync += async e => await OnDisconnectedAsync(e, cancellationToken).ConfigureAwait(false);
            _mqttClient.ConnectedAsync += async _ => await OnConnectedAsync(cancellationToken).ConfigureAwait(false);
            _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

            await ConnectAsync(cancellationToken).ConfigureAwait(false);
            _initializationTask.TrySetResult(true);
        }
        catch (Exception ex)
        {
            _initializationTask.TrySetException(ex);
            Logger.Error(ex, "Failed to initialize MQTT client");
        }
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        Logger.Information("# DISCONNECTED FROM SERVER #");

        if (_shutdown || cancellationToken.IsCancellationRequested)
        {
            Logger.Information("# NO RECONNECTION BECAUSE OF SHUTDOWN #");
            return;
        }

        if (Reason.Equals(eventArgs.ReasonString, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Information("# NO RECONNECTION BECAUSE OF INTENTIONALLY DISCONNECTION #");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

        try
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Logger.Information("MQTT reconnect cancelled");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "# RECONNECTING FAILED ##");
        }
    }

    private async Task OnConnectedAsync(CancellationToken cancellationToken)
    {
        Logger.Information("# CONNECTED WITH SERVER #");
        await SendLwtConnectedAsync(cancellationToken).ConfigureAwait(false);

        string topic = $"{PoolControlConfig.Instance.Settings.BaseTopic.Command}#";
        await SubscribeAsync(topic, cancellationToken).ConfigureAwait(false);
    }

    private Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        Logger.Information("# Received Topic={Topic} Payload={Payload} QoS={Qos} Retain={Retain}",
            e.ApplicationMessage.Topic,
            Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()),
            e.ApplicationMessage.QualityOfServiceLevel,
            e.ApplicationMessage.Retain);
        return Task.CompletedTask;
    }

    private async Task SendLwtConnectedAsync(CancellationToken cancellationToken)
    {
        string lwtTopic = PoolControlConfig.Instance.Settings!.LWT.Topic;
        string? lwtConnectMessage = PoolControlConfig.Instance.Settings.LWT.ConnectMessage;
        await PublishMessage(lwtTopic, lwtConnectMessage, 2, true, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendLwtDisconnectedAsync(CancellationToken cancellationToken)
    {
        string lwtTopic = PoolControlConfig.Instance.Settings!.LWT.Topic;
        string? lwtDisconnectMessage = PoolControlConfig.Instance.Settings.LWT.DisconnectMessage;
        await PublishMessage(lwtTopic, lwtDisconnectMessage, 2, true, cancellationToken).ConfigureAwait(false);
        Logger.Information("Topic: {Topic} Payload: {Payload}", lwtTopic, lwtDisconnectMessage);
    }

    public void Register(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
    {
        if (_mqttClient != null)
        {
            _mqttClient.ApplicationMessageReceivedAsync += handler;
        }
    }

    public void UnRegister(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
    {
        if (_mqttClient != null)
        {
            _mqttClient.ApplicationMessageReceivedAsync -= handler;
        }
    }

    public async Task SubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (_mqttClient == null)
        {
            Logger.Warning("Cannot subscribe before MQTT client is initialized");
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            topic = Win + topic;
        }

        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build(), cancellationToken).ConfigureAwait(false);
        Logger.Information("# Subscribed topic={Topic}", topic);
    }

    public async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (_mqttClient == null)
        {
            Logger.Warning("Cannot unsubscribe before MQTT client is initialized");
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            topic = Win + topic;
        }

        await _mqttClient.UnsubscribeAsync(topic, cancellationToken).ConfigureAwait(false);
        Logger.Information("# Unsubscribed topic={Topic}", topic);
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient == null || _options == null)
        {
            Logger.Warning("MQTT client has not been configured yet");
            return;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mqttClient.IsConnected || _shutdown || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Logger.Information("# CONNECTING ... #");
            await _mqttClient.ConnectAsync(_options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public void Disconnect()
    {
        try
        {
            _shutdown = true;
            DisconnectAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();
            Logger.Information("# Disconnect started because of shutdown");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "# DISCONNECTING FAILED #");
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_mqttClient == null)
        {
            return;
        }

        _shutdown = true;
        await SendLwtDisconnectedAsync(cancellationToken).ConfigureAwait(false);

        // Build a minimal disconnect options (no MQTTv5-only fields) and disconnect
        await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken).ConfigureAwait(false);

        _cancellationTokenSource.Cancel();
    }

    public Task PublishAsync(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default)
    {
        return PublishMessage(topic, payload, qos, retain, cancellationToken);
    }

    public Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        return ConnectAsync(cancellationToken);
    }

    public async Task PublishMessage(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default)
    {
        if (_mqttClient == null)
        {
            Logger.Warning("Cannot publish before MQTT client is initialized");
            return;
        }

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

        await _mqttClient.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        Logger.Information("# Published  Topic={Topic} Payload={Payload} QoS={Qos} Retain={Retain}", message.Topic, Encoding.UTF8.GetString(message.PayloadSegment.ToArray()), message.QualityOfServiceLevel, message.Retain);

        Process currentProc = Process.GetCurrentProcess();
        double bytesInUse = currentProc.PrivateMemorySize64 / 1024.0 / 1024.0;

        Logger.Debug("MBytes in use {Bytes} in {Proc}", bytesInUse, currentProc.ProcessName);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cancellationTokenSource.Cancel();
        _mqttClient?.Dispose();
        _connectionLock.Dispose();
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }
}