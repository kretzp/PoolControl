using MQTTnet.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PoolControl.Communication;

public interface IPoolMqttClient : IDisposable
{
    Task PublishAsync(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default);
    Task PublishMessage(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default);
    Task SubscribeAsync(string topic, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
    Task EnsureConnectedAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    void Register(Func<MqttApplicationMessageReceivedEventArgs, Task> handler);
    void UnRegister(Func<MqttApplicationMessageReceivedEventArgs, Task> handler);
}