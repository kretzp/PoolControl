using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Serilog;

namespace PoolControl.Helper;

public class Persistence
{
    private static Persistence? _instance;
    private static readonly object Padlock = new();

    private bool _persistenceInUse;

    public static Persistence Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new Persistence(Log.Logger);
            }
        }
    }

    protected ILogger Logger { get; init; }

    private string PersistenceFile { get; set; }

    private Persistence(ILogger? logger)
    {
        Logger = logger?.ForContext<Persistence>() ?? throw new ArgumentNullException(nameof(logger));
        PersistenceFile = PoolControlConfig.Instance.Settings!.PersistenceFile;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            PersistenceFile = "win" + PersistenceFile;
        }
    }

    public string Serialize(object? o)
    {
        var json = "";
        try
        {
            json = JsonConvert.SerializeObject(o, Formatting.Indented);
            Logger.Information("Persistence Serialized {Json}", json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Serialize Exception:");
        }

        return json;
    }

    public void Save(object? o)
    {
        try
        {
            while (_persistenceInUse)
            {
            }

            _persistenceInUse = true;
            using (StreamWriter file = File.CreateText(PersistenceFile))
            {
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                serializer.Serialize(file, o);
            }
            Logger.Information("Persistence Saved {O}", o);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Save Exception:");
        }
        finally
        {
            _persistenceInUse = false;
        }
    }

    public T? Load<T>()
    {
        try
        {
            while (_persistenceInUse)
            {
            }

            _persistenceInUse = true;
            using var file = File.OpenText(PersistenceFile);
            var serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented
            };
            var o = serializer.Deserialize<T>(new JsonTextReader(file));
            Logger.Information("Persistence Loaded {O}", o);
            return o;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Load Exception:");
            return Activator.CreateInstance<T>();
        }
        finally
        {
            _persistenceInUse=false;
        }
    }
}