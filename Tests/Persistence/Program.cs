using PoolControl.Helper;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
var previousDirectory = Environment.CurrentDirectory;
var testDirectory = Path.Combine(Path.GetTempPath(), "PoolControl-Persistence-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testDirectory);
Environment.CurrentDirectory = testDirectory;
try
{
    var persistence = Persistence.Instance;
    using (var entered = new ManualResetEventSlim())
    using (var release = new ManualResetEventSlim())
    using (var readerStarted = new ManualResetEventSlim())
    {
        var save = Task.Run(() => persistence.Save(new BlockingDocument(entered, release)));
        Assert(entered.Wait(TimeSpan.FromSeconds(5)), "Writer did not enter serialization");
        var load = Task.Run(() => { readerStarted.Set(); return persistence.Load<Document>(); });
        try
        {
            Assert(readerStarted.Wait(TimeSpan.FromSeconds(5)), "Reader did not start");
            Assert(!load.Wait(TimeSpan.FromMilliseconds(100)), "Reader entered an incomplete write");
        }
        finally { release.Set(); }
        await Task.WhenAll(save, load).WaitAsync(TimeSpan.FromSeconds(5));
        Assert(load.Result?.Sequence == 7, "Reader did not receive the completed document");
        Assert(Log.Errors.Count == 0, "Concurrent reader encountered an I/O error");
        Console.WriteLine("PASS: load waits for an active save and reads the complete JSON");
    }

    persistence.Save(new Document { Sequence = 0, Payload = "0" });
    using (var start = new ManualResetEventSlim())
    {
        var workers = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            start.Wait();
            for (int i = 0; i < 60; i++)
            {
                int sequence = worker * 60 + i;
                persistence.Save(new Document { Sequence = sequence, Payload = sequence.ToString() });
                var loaded = persistence.Load<Document>();
                Assert(loaded != null && loaded.Payload == loaded.Sequence.ToString(), "Concurrent access returned partial/default data");
            }
        })).ToArray();
        start.Set();
        await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(30));
        Assert(Log.Errors.Count == 0, "Concurrent access caused I/O or serialization errors");
        Console.WriteLine("PASS: 480 parallel save/load cycles preserve complete documents without I/O errors");
    }

    int errors = Log.Errors.Count;
    persistence.Save(new Document { Sequence = 98, Payload = "preserved" });
    persistence.Save(new BrokenDocument());
    Assert(Log.Errors.Count == errors + 1, "Expected serialization error was not logged");
    var preserved = persistence.Load<Document>();
    Assert(preserved?.Sequence == 98 && preserved.Payload == "preserved", "Failed serialization damaged the previous document");
    Assert(!Directory.EnumerateFiles(testDirectory, "*.tmp").Any(), "Failed serialization left a temporary file behind");
    await Task.Run(() => persistence.Save(new Document { Sequence = 99, Payload = "99" })).WaitAsync(TimeSpan.FromSeconds(5));
    Assert(persistence.Load<Document>()?.Sequence == 99, "Save exception did not release the lock");
    Console.WriteLine("PASS: failed save preserves the previous document, cleans up and releases the lock");

    var file = Path.Combine(testDirectory, OperatingSystem.IsWindows() ? "winstate.json" : "state.json");
    File.WriteAllText(file, "{broken JSON");
    errors = Log.Errors.Count;
    Assert(persistence.Load<Document>()?.Sequence == -1 && Log.Errors.Count == errors + 1, "Load fallback changed");
    await Task.Run(() => persistence.Save(new Document { Sequence = 100, Payload = "100" })).WaitAsync(TimeSpan.FromSeconds(5));
    Assert(persistence.Load<Document>()?.Sequence == 100, "Load exception did not release the lock");
    Console.WriteLine("PASS: failed load preserves fallback behavior and releases the lock");
}
finally { Environment.CurrentDirectory = previousDirectory; }
Console.WriteLine("4 persistence checks passed. Isolated files: " + testDirectory);

public class Document { public int Sequence { get; set; } = -1; public string? Payload { get; set; } }
public class BlockingDocument(ManualResetEventSlim entered, ManualResetEventSlim release)
{
    public int Sequence { get { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Test release timed out"); return 7; } }
}
public class BrokenDocument { public int Sequence => throw new InvalidOperationException("Expected test failure"); }

namespace PoolControl.Helper
{
    public class TestErrorSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent) { if (logEvent.Level >= LogEventLevel.Error) Log.Errors.Enqueue(logEvent); }
    }
    public static class Log
    {
        public static ConcurrentQueue<LogEvent> Errors { get; } = new();
        public static Serilog.ILogger Logger { get; } = new Serilog.LoggerConfiguration().WriteTo.Sink(new TestErrorSink()).CreateLogger();
    }
    public class PoolControlConfig
    {
        public static PoolControlConfig Instance { get; } = new();
        public TestSettings Settings { get; } = new();
    }
    public class TestSettings { public string PersistenceFile => "state.json"; }
}
