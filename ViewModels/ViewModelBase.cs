using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Newtonsoft.Json;
using System;
using PoolControl.Communication;
using Serilog;
using System.Reflection;
using System.Collections;
using PoolControl.Helper;
using System.Threading;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ViewModelBase : ReactiveObject, IDisposable
    {
        private bool disposedValue;

        protected ILogger Logger { get; set; }

        public ViewModelBase()
        {
            Logger = Log.Logger?.ForContext<ViewModelBase>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        [JsonIgnore]
        protected Timer? Timer { get; private set; }

        [Reactive]
        [JsonProperty]
        public int IntervalInSec { get; set; }

        [JsonIgnore]
        public int Interval { get { return IntervalInSec * 1000; } }

        [Reactive]
        [JsonProperty]
        public string Name { get; set; }

        [JsonIgnore]
        public string LocationName
        {
            get
            {
                string ret = "Nix";
                try
                {
                    ret = (string)typeof(Resource).GetProperty(Name).GetValue(null);
                }
                catch (Exception) { }

                return ret;
            }
        }

        public void RestartTimerAndPublishNewInterval()
        {
            Timer = RestartTimer(Timer, OnTimerTicked, Interval);

            publishMessageWithType(PoolControlHelper.GetPropertyName(() => IntervalInSec), IntervalInSec.ToString());
        }

        protected abstract void OnTimerTicked(object? state);

        protected Timer RestartTimer(Timer timer, TimerCallback callback, int interval)
        {
            return RestartTimer(timer, callback, interval, interval);
        }

        protected Timer RestartTimer(Timer timer, TimerCallback callback, int duetime, int interval)
        {
            if (timer == null && interval > 0)
            {
                var autoEvent = new AutoResetEvent(false);
                timer = new Timer(callback, autoEvent, duetime, interval);
            }
            else
            {
                if (interval <= 0)
                {
                    if (Timer != null)
                    {
                        timer.Dispose();
                        timer = null;
                        Logger.Debug("Timer deleted");
                    }
                }
                else
                {
                    if (timer.Change(duetime, interval))
                    {
                        Logger.Debug($"Timer set: duetime {duetime} interval {interval}");
                    }
                    else
                    {
                        Logger.Debug($"Timer set: duetime {duetime} interval {interval} error");
                    }
                }
            }

            return timer;
        }

        public void publishMessage(string propertyName, string value, int qos, bool retain, bool reallySend)
        {
            if(!reallySend)
            {
                Logger.Warning($"Property {propertyName} should not be sended for value {value}. MQTT Message will not be published.");
                return;
            }
            if (propertyName == null)
            {
                Logger.Warning($"Property is null for value {value}. MQTT Message will not be published.");
            }
            else
            {
                _ = PoolMqttClient.Instance.publishMessage($"{PoolControlConfig.Instance.Settings.BaseTopic.State}{propertyName}", value, qos, retain);
            }
        }

        public void publishMessage(string propertyName, string value, int qos, bool retain)
        {
            publishMessage(propertyName, value, qos, retain, true);
        }

        public void publishMessage(string propertyName, string value)
        {
            publishMessage(propertyName, value, true);
        }

        public void publishMessage(string propertyName, string value, bool reallySend)
        {
            publishMessage(propertyName, value, 0, false, reallySend);
        }

        public void publishMessageWithType(string propertyName, string value)
        {
            publishMessage($"{this.GetType().Name}/{propertyName}", value, true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
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

                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~ViewModelBase()
        // {
        //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
