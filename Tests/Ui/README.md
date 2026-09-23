# Native UI integration checks

```powershell
dotnet build Tests/Ui/Ui.Tests.csproj -p:UsedAvaloniaProducts= -p:UseAppHost=false -o artifacts/ui-test
dotnet artifacts/ui-test/Ui.Tests.dll
```

The executable tests use Avalonia.Headless + Skia at 1024 × 600. They load the real application styles, views and view models, but do not start the hardware lifecycle. An isolated settings file points the child models' legacy MQTT singleton at localhost port 1; no production settings or credentials are loaded. The test application must be built into a **dedicated directory**: it writes its test appsettings.json next to its executable.

Checks cover preference persistence/fallback, German number entry, input validation, time entry, concurrent external changes, all six pages in German/English and Light/Dark/System, edit/apply, repeated Modern/Classic switches with the same view model and unchanged equipment settings. Rendered PNGs are saved under `artifacts/ui-test/ui-test-data/` for visual inspection. No GPIO or sensor commands are executed.

Further checks cover solar/lamp status, sensor addresses and text fields, persisted
settings after reload, UI versus MQTT number formatting, excessive precision,
measurement-age units and nested dialog navigation (close, apply and Escape).
For all nine executable checks use [Run-All.ps1](../Run-All.ps1); see the
[test overview](../README.md) for coverage and limitations.

`UsedAvaloniaProducts=` suppresses the build statistics task for sandboxed builds; it does not change application behavior. `UseAppHost=false` and the separate output avoid replacing a running application executable.
