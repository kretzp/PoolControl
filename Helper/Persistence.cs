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

    private readonly object _persistenceLock = new();

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
        lock (_persistenceLock)
        {
            string? temporaryFile = null;
            try
            {
                var destinationFile = Path.GetFullPath(PersistenceFile);
                var directory = Path.GetDirectoryName(destinationFile)
                    ?? throw new InvalidOperationException($"Persistence path '{destinationFile}' has no directory.");
                Directory.CreateDirectory(directory);
                temporaryFile = Path.Combine(directory,
                    $".{Path.GetFileName(destinationFile)}.{Guid.NewGuid():N}.tmp");

                using (var stream = new FileStream(temporaryFile, FileMode.CreateNew, FileAccess.Write,
                           FileShare.None, 4096, FileOptions.WriteThrough))
                using (var file = new StreamWriter(stream))
                {
                    var serializer = new JsonSerializer
                    {
                        Formatting = Formatting.Indented
                    };
                    serializer.Serialize(file, o);
                    file.Flush();
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporaryFile, destinationFile, overwrite: true);
                temporaryFile = null;
                Logger.Information("Persistence Saved {O}", o);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Save Exception:");
            }
            finally
            {
                if (temporaryFile != null)
                {
                    try { File.Delete(temporaryFile); }
                    catch (Exception ex) { Logger.Warning(ex, "Could not delete temporary persistence file {File}", temporaryFile); }
                }
            }
        }
    }

    public T? Load<T>()
    {
        lock (_persistenceLock)
        {
            try
            {
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
        }
    }
}
