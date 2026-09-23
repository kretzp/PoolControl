# Shutdown regression checks

```powershell
dotnet run --project Tests/Shutdown/Shutdown.Tests.csproj
dotnet run --project Tests/DistanceMeasurement/DistanceMeasurement.Tests.csproj
```

Links the production timer lifecycle and time trigger implementations. No UI,
MQTT or hardware is started. Verifies draining all callbacks, idempotent stop,
disabled-but-active timers, pending callbacks and prevention of trigger rearming.
The distance measurement checks additionally verify cooperative shutdown.
