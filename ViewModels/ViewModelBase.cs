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

    protected ILogger Logger { get; set; }

    protected ViewModelBase()
    {
        Logger = Log.Logger?.ForContext<ViewModelBase>() ?? throw new ArgumentNullException(nameof(Logger));
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
        get
        {
            var ret = "Nix";
            try
            {
                ret = (string)typeof(Resource).GetProperty(Name)?.GetValue(null)!;
            }
            catch (Exception)
            {
                // ignored
            }

            return ret;
        }
    }

    public void RestartTimerAndPublishNewInterval()
    {
        Timer = RestartTimer(Timer, OnTimerTicked, Interval);

        PublishMessageWithType(PoolControlHelper.GetPropertyName(() => IntervalInSec), IntervalInSec.ToString(), false);
    }

    protected abstract void OnTimerTicked(object? state);

    protected Timer? RestartTimer(Timer? timer, TimerCallback callback, int interval)
    {
        return RestartTimer(timer, callback, interval, interval);
    }

    protected Timer? RestartTimer(Timer? timer, TimerCallback callback, int dueTime, int interval)
    {
        if (timer == null && interval > 0)
        {
            var autoEvent = new AutoResetEvent(false);
            timer = new Timer(callback, autoEvent, dueTime, interval);
        }
        else
        {
            if (interval <= 0)
            {
                if (Timer != null)
                {
                    timer?.Dispose();
                    timer = null;
                    Logger.Debug("Timer deleted");
                }
            }
            else
            {
                if (timer != null && timer.Change(dueTime, interval))
                {
                    Logger.Debug("Timer set: dueTime {DueTime} interval {Interval}", dueTime, interval);
                }
                else
                {
                    Logger.Debug("Timer set: dueTime {DueTime} interval {Interval} error", dueTime, interval);
                }
            }
        }

        return timer;
    }

    public void PublishMessage(string? propertyName, string? value, int qos, bool retain, bool reallySend, bool notifyOnReallySend = false)
    {
        if(!reallySend)
        {
            Logger.Warning("Property {PropertyName} should not be sent for value {Value}. MQTT Message will not be published", propertyName, value);
            return;
        }
        if (propertyName == null)
        {
            Logger.Warning("Property is null for value {Value}. MQTT Message will not be published", value);
        }
        else
        {
            _ = PoolMqttClient.Instance.publishMessage($"{PoolControlConfig.Instance.Settings?.BaseTopic.State}{propertyName}", value, qos, retain);
            if (notifyOnReallySend)
            {
                ShowNotification($"MQTT: {PoolControlConfig.Instance.Settings?.BaseTopic.State}", $"Property: {propertyName} Payload: {value}");
            }
        }
    }

    public void PublishMessage(string propertyName, string? value, int qos, bool retain, bool notifyOnReallySend = false)
    {
        PublishMessage(propertyName, value, qos, retain, true, notifyOnReallySend);
    }

    public void PublishMessage(string propertyName, string? value, bool notifyOnReallySend = false)
    {
        PublishMessage(propertyName, value, true, notifyOnReallySend);
    }

    public void PublishMessage(string propertyName, string? value, bool reallySend, bool notifyOnReallySend = false)
    {
        PublishMessage(propertyName, value, 0, false, reallySend, notifyOnReallySend);
    }

    public void PublishMessageWithType(string propertyName, string? value, bool notifyOnReallySend = false)
    {
        PublishMessage($"{this.GetType().Name}/{propertyName}", value, true, notifyOnReallySend);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;
        if (disposing)
        {
            // Dispose stuff
        }

        foreach (PropertyInfo pi in GetType().GetProperties())
        {
            if(pi.GetValue(this) is IEnumerable enumerable)
            {
                foreach(var item in enumerable)
                {
                    if(item is IDisposable dp)
                    {
                        dp.Dispose();
                    }
                }
            }

            if (pi.GetValue(this) is IDisposable disposable)
            {
                disposable.Dispose();
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