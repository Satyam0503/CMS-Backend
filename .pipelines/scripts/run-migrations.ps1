# Run database migrations against the target MongoDB.
# Invoked from the deploy pipeline AFTER files are in place and BEFORE the IIS app pool starts,
# so a failed migration aborts the deployment with the previous version still serving traffic.
#
# The migrations runner (Codeji.CMS.Migrations) reads its MongoDB connection string from
# appsettings.json next to the published DLL — this script overwrites that file with the
# environment-specific connection string before invoking the runner.
#
# Usage:
#   .\run-migrations.ps1 `
#     -MigrationsPath "D:\Hosting\Codeji\crm-test\backend\migrations" `
#     -MongoConnectionString "mongodb://user:pass@host:27017/dbname"

param(
    [Parameter(Mandatory = $true)]
    [string]$MigrationsPath,

    [Parameter(Mandatory = $true)]
    [string]$MongoConnectionString
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $MigrationsPath)) {
    throw "Migrations path not found: $MigrationsPath"
}

$dll = Join-Path $MigrationsPath "Codeji.CMS.Migrations.dll"
if (-not (Test-Path $dll)) {
    throw "Migrations DLL not found at: $dll"
}

Write-Host "=========================================="
Write-Host "Running database migrations"
Write-Host "   Path: $MigrationsPath"
Write-Host "=========================================="

# Overwrite appsettings.json with the environment's connection string.
# Format must match the shape Program.cs reads:
#   { "ConnectionStrings": { "mongodb": "..." } }
# Use [ordered] so JSON keys appear in the same order on every run, and write via
# File.WriteAllText so the output is UTF-8 without BOM (Out-File -Encoding UTF8 on
# Windows PowerShell 5.1 prepends a BOM).
$appsettings = [ordered]@{
    ConnectionStrings = [ordered]@{
        mongodb = $MongoConnectionString
    }
}
$appsettingsPath = Join-Path $MigrationsPath "appsettings.json"
$json = $appsettings | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($appsettingsPath, $json, [System.Text.UTF8Encoding]::new($false))

# Invoke the runner. The runner is a console app — stdout streams here, exit code propagates.
Push-Location $MigrationsPath
try {
    & dotnet "Codeji.CMS.Migrations.dll"
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exitCode -ne 0) {
    throw "Migrations failed with exit code $exitCode. Deployment aborted; IIS app pool will not be started."
}

Write-Host "=========================================="
Write-Host "Migrations completed successfully"
Write-Host "=========================================="
