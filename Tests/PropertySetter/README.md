# PropertySetter regression checks

```powershell
dotnet run --project Tests/PropertySetter/PropertySetter.Tests.csproj
```

The test links the production property setter and verifies direct root,
nested-object and dictionary-backed updates plus invalid input handling.
