using MQTTnet.Client;
using PoolControl.Communication;
using PoolControl.Helper;
using PoolControl.ViewModels;

void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var mqtt = new RecordingMqttClient();
var model = new ProbeViewModel(mqtt);
model.IntervalInSec = 60;

var invalidModel = new ProbeViewModel(mqtt);
Assert(ConfigurationValidator.Validate(new object?[] { invalidModel }).Count == 1,
    "A zero interval for a timer-driven model passed configuration validation.");
Assert(ConfigurationValidator.Validate(new object?[] { model }).Count == 0,
    "A valid timer interval failed configuration validation.");

Assert(model.Starts == 0 && mqtt.Publications == 0,
    "Construction activated background work or external access.");
await model.PublishProbeAsync();
Assert(mqtt.Publications == 0, "An inactive view model published an MQTT message.");

model.Start();
model.Start();
Assert(model.Starts == 1, "Start was not idempotent.");
var publicationsAfterStart = mqtt.Publications;
await model.PublishProbeAsync();
Assert(mqtt.Publications == publicationsAfterStart + 1, "An active view model did not publish through its configured client.");

await model.StopAsync();
await model.PublishProbeAsync();
Assert(mqtt.Publications == publicationsAfterStart + 1, "A stopped view model still performed external access.");
Console.WriteLine("PASS: constructors stay inactive; Start is explicit and idempotent; Stop blocks later access");

internal sealed class ProbeViewModel(IPoolMqttClient mqttClient) : ViewModelBase(mqttClient), IUsesIntervalTimer
{
    public int Starts { get; private set; }
    public Task PublishProbeAsync() => PublishMessageAsync("probe", "1");
    protected override void OnStarted()
    {
        Starts++;
        base.OnStarted();
    }
    protected override void OnTimerTicked(object? state) { }
}

internal sealed class RecordingMqttClient : IPoolMqttClient
{
    public int Publications { get; private set; }
    public Task PublishAsync(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default)
    {
        Publications++;
        return Task.CompletedTask;
    }
    public Task PublishMessage(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default)
        => PublishAsync(topic, payload, qos, retain, cancellationToken);
    public Task SubscribeAsync(string topic, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnsureConnectedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Register(Func<MqttApplicationMessageReceivedEventArgs, Task> handler) { }
    public void UnRegister(Func<MqttApplicationMessageReceivedEventArgs, Task> handler) { }
    public void Dispose() { }
}
