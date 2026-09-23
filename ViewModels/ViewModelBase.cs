using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Newtonsoft.Json;
using System;
using PoolControl.Communication;
using Serilog;
using System.Reflection;
using System.Collections;
using System.Threading;
using PoolControl.Helper;
using Log = PoolControl.Helper.Log;
using PoolControl.Views;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace PoolControl.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public abstract class ViewModelBase : ReactiveObject, IDisposable
{
    private bool _disposedValue;
    private int _started;
    private readonly PoolControl.Time.TimerLifetime _timerLifetime = new();

    protected bool IsStarted => Volatile.Read(ref _started) == 1;

    public virtual void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1) return;
        OnStarted();
    }

    protected virtual void OnStarted() => RestartTimerAndPublishNewInterval();

    public virtual Task StopAsync()
    {
        Interlocked.Exchange(ref _started, 0);
        return _timerLifetime.StopAsync();
    }

    protected ILogger Logger { get; set; }

    protected IPoolMqttClient MqttClient { get; }

    protected ViewModelBase(IPoolMqttClient? mqttClient = null)
    {
        Logger = Log.Logger?.ForContext<ViewModelBase>() ?? throw new ArgumentNullException(nameof(Logger));
        MqttClient = mqttClient ?? PoolMqttClient.Instance;
    }

    private static void ShowNotification(string title, string message)
    {
        Dispatcher.UIThread.Post(() => { AsyncShowNotification(title, message); }); 
    }

    private static void AsyncShowNotification(string title, string message)
    {
        (App.MainWindow as MainWindow)?.ShowNotification(title, message);
    }

    [JsonIgnore]
    protected Timer? Timer { get; private set; }

    [Reactive]
    [JsonProperty]
    [TimerInterval]
    public int IntervalInSec { get; set; }

    [JsonIgnore]
    public int Interval => IntervalInSec * 1000;

    [Reactive]
    [JsonProperty]
    public string Name { get; set; } = "Name";

    [Reactive]
    [JsonProperty]
    public bool WinterMode { get; set; }


    [JsonIgnore]
    public string LocationName
    {
        get => Resource.ResourceManager.GetString(Name ?? "", Resource.Culture) ?? Name ?? "";
    }

    public void RestartTimerAndPublishNewInterval()
    {
        if (!IsStarted) return;
        Timer = RestartTimer(Timer, OnTimerTicked, Interval);

        _ = PublishMessageWithTypeAsync(PoolControlHelper.GetPropertyName(() => IntervalInSec), IntervalInSec.ToString(), false);
    }

    protected abstract void OnTimerTicked(object? state);

    protected Timer? RestartTimer(Timer? timer, TimerCallback callback, int interval)
    {
        return RestartTimer(timer, callback, interval, interval);
    }

    protected Timer? RestartTimer(Timer? timer, TimerCallback callback, int dueTime, int interval)
    {
        return _timerLifetime.Restart(timer, callback, TimeSpan.FromMilliseconds(dueTime), TimeSpan.FromMilliseconds(interval));
    }

    public Task PublishMessageAsync(string? propertyName, string? value, int qos, bool retain, bool reallySend, bool notifyOnReallySend = false)
    {
        if (!IsStarted) return Task.CompletedTask;
        if (!reallySend)
        {
            Logger.Warning("Property {PropertyName} should not be sent for value {Value}. MQTT Message will not be published", propertyName, value);
            return Task.CompletedTask;
        }

        if (propertyName == null)
        {
            Logger.Warning("Property is null for value {Value}. MQTT Message will not be published", value);
            return Task.CompletedTask;
        }

        var task = MqttClient.PublishAsync($"{PoolControlConfig.Instance.Settings?.BaseTopic.State}{propertyName}", value, qos, retain);
        if (notifyOnReallySend)
        {
            ShowNotification($"MQTT: {PoolControlConfig.Instance.Settings?.BaseTopic.State}", $"Property: {propertyName} Payload: {value}");
        }

        return task;
    }

    public Task PublishMessageAsync(string propertyName, string? value, int qos, bool retain, bool notifyOnReallySend = false)
    {
        return PublishMessageAsync(propertyName, value, qos, retain, true, notifyOnReallySend);
    }

    public Task PublishMessageAsync(string propertyName, string? value, bool notifyOnReallySend = false)
    {
        return PublishMessageAsync(propertyName, value, true, notifyOnReallySend);
    }

    public Task PublishMessageAsync(string propertyName, string? value, bool reallySend, bool notifyOnReallySend = false)
    {
        return PublishMessageAsync(propertyName, value, 0, false, reallySend, notifyOnReallySend);
    }

    public Task PublishMessageWithTypeAsync(string propertyName, string? value, bool notifyOnReallySend = false)
    {
        return PublishMessageAsync($"{this.GetType().Name}/{propertyName}", value, true, notifyOnReallySend);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;

        if (disposing)
        {
            // Dispose managed resources explicitly. Derived types should override
            // this method if they hold additional disposable resources.
            try
            {
                StopAsync().GetAwaiter().GetResult();
                Timer = null;
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Error disposing ViewModelBase managed resources");
            }
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        // Dispose
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
