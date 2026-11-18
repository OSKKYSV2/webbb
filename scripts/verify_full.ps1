# Run from Windows PowerShell 5.1 or PowerShell 7 with .NET 10 SDK installed.
# Example: powershell -ExecutionPolicy Bypass -File scripts/verify_full.ps1
# To apply migrations to your configured database too: add -ApplyDatabase
param([switch]$ApplyDatabase)

$ErrorActionPreference = 'Stop'
Set-Location (Resolve-Path (Join-Path $PSScriptRoot '..'))

function Invoke-Checked {
    param([string[]]$Arguments)
    Write-Host "`n> dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed (exit $LASTEXITCODE): dotnet $($Arguments -join ' ')"
    }
}

Invoke-Checked -Arguments @('restore', 'VehicleRegistry.slnx')
Invoke-Checked -Arguments @('tool', 'restore')
Invoke-Checked -Arguments @('build', 'VehicleRegistry.slnx', '--configuration', 'Release', '--no-restore')
Invoke-Checked -Arguments @('test', 'tests/VehicleRegistry.Tests/VehicleRegistry.Tests.csproj', '--configuration', 'Release', '--no-build')
Invoke-Checked -Arguments @('ef', 'migrations', 'has-pending-model-changes', '--project', 'src/VehicleRegistry.Infrastructure', '--startup-project', 'src/VehicleRegistry.Web', '--configuration', 'Release', '--no-build')
Invoke-Checked -Arguments @('ef', 'migrations', 'script', '--idempotent', '--project', 'src/VehicleRegistry.Infrastructure', '--startup-project', 'src/VehicleRegistry.Web', '--configuration', 'Release', '--no-build', '--output', 'upgrade.sql')
Invoke-Checked -Arguments @('package', 'list', '--project', 'VehicleRegistry.slnx', '--vulnerable', '--include-transitive')
if ($ApplyDatabase) {
    Invoke-Checked -Arguments @('ef', 'database', 'update', '--project', 'src/VehicleRegistry.Infrastructure', '--startup-project', 'src/VehicleRegistry.Web', '--configuration', 'Release', '--no-build')
    Write-Host 'Migrace aplikované do nastavene databaze.' -ForegroundColor Green
} else {
    Write-Host 'Migrace nebyly aplikovany. Pro database update spustte znovu s -ApplyDatabase.' -ForegroundColor Yellow
}
Write-Host "`nPreflight dotnet prikazy prosly. Manualni testy v prohlizeci jsou dalsi krok." -ForegroundColor Green
