# MQTT topic regression checks

```powershell
dotnet run --project Tests/MqttTopic/MqttTopic.Tests.csproj
```

Links the production MQTT topic helper and verifies outgoing Linux/Windows
prefixes, command-path normalization and rejection of unrelated topics.
