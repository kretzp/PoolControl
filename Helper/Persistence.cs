using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoolControl.Helper
{
    public class Persistence
    {
        private static Persistence? _instance;
        private static readonly object padlock = new object();

        private bool persistenceInUse = false;

        public static Persistence Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new Persistence(Log.Logger);
                    }
                    return _instance;
                }
            }
        }

        protected ILogger Logger { get; set; }

        protected string PersistenceFile { get; private set; }

        private Persistence(ILogger logger)
        {
            Logger = logger?.ForContext<Persistence>() ?? throw new ArgumentNullException(nameof(Logger));
            PersistenceFile = PoolControlConfig.Instance.Settings.PersistenceFile;
        }

        public string Serialize(object o)
        {
            string json = "";
            try
            {
                json = JsonConvert.SerializeObject(o, Formatting.Indented);
                Logger.Information("Persistence Serialized {json}", json);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Serialize Exception:");
            }

            return json;
        }

        public void Save(object o)
        {
            try
            {
                while (persistenceInUse) ;
                persistenceInUse = true;
                using StreamWriter file = File.CreateText(PersistenceFile);
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, o);
                Logger.Information("Persistence Saved {o}", o);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Save Exception:");
            }
            finally
            {
                persistenceInUse = false;
            }
        }

        public T Load<T>()
        {
            try
            {
                while (persistenceInUse) ;
                persistenceInUse = true;
                using StreamReader file = File.OpenText(PersistenceFile);
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                T o = serializer.Deserialize<T>(new JsonTextReader(file));
                Logger.Information("Persistence Loaded {o}", o);
                return o;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Load Exception:");
                return Activator.CreateInstance<T>();
            }
            finally
            {
                persistenceInUse=false;
            }
        }
    }
}
