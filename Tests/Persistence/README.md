# Persistence regression checks

```powershell
dotnet run --project Tests/Persistence/Persistence.Tests.csproj
```

Links the production Persistence class with isolated configuration/logging doubles.
Files are written to a new temporary directory, never to application configuration.
Checks cover an active write blocking a reader, concurrent save/load operations,
and lock release after save/load errors. No MQTT, UI or hardware is started.
