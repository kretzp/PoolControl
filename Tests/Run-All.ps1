$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$checks = @('DistanceMeasurement', 'EzoCommand', 'MeasurementOrder', 'MqttTopic', 'Persistence', 'PropertySetter', 'Shutdown', 'StartupLifecycle', 'Ui')
foreach ($check in $checks) {
    $project = Join-Path $PSScriptRoot "$check/$check.Tests.csproj"
    $output = Join-Path $repoRoot "artifacts/checks/$check"
    Write-Host "Checking $check"
    & dotnet build $project -c Release '-p:UsedAvaloniaProducts=' '-p:UseAppHost=false' -o $output --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $check" }
    if ($check -eq 'StartupLifecycle') {
        # Prevent production logging endpoints from being loaded by the real model.
        '{"Serilog":{"MinimumLevel":"Fatal"},"Settings":{"PersistenceFile":"startup-fixture.json","PersistenceSaveIntervalInSec":60,"BaseTopic":{"Command":"startup-test/cmd/","State":"startup-test/state/"},"MQTT":{"Server":"127.0.0.1","Port":1}}}' | Set-Content -LiteralPath (Join-Path $output 'appsettings.json') -Encoding utf8
    }
    Push-Location $output
    try {
        & dotnet (Join-Path $output "$check.Tests.dll")
        if ($LASTEXITCODE -ne 0) { throw "Checks failed: $check" }
    }
    finally { Pop-Location }
}
Write-Host 'All 9 test executables passed.'
