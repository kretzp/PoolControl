# DistanceMeasurement regression checks

Run from the repository root:

```powershell
dotnet run --project Tests/DistanceMeasurement/DistanceMeasurement.Tests.csproj
```

The executable links the production measurement sources and injects simulated GPIO.
UI models and logging initialization are replaced with isolated test doubles to avoid
starting timers, MQTT or real hardware. Checks cover both echo timeouts, recovery,
successful averaging, overlapping calls, GPIO exceptions and invalid sample counts.
An outer three-second deadline makes the checks fail if an unbounded loop returns.
