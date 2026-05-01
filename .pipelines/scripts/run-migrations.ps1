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
#     -MigrationsPath "C:\inetpub\sites\crm-test\backend\migrations" `
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
# The runner's ConfigurationBuilder reads only this file (see Codeji.CMS.Migrations/Program.cs),
# so the connection string committed in source is irrelevant once we land here.
$appsettings = @{
    ConnectionStrings = @{
        mongodb = $MongoConnectionString
    }
}
$appsettingsPath = Join-Path $MigrationsPath "appsettings.json"
$appsettings | ConvertTo-Json -Depth 10 | Out-File $appsettingsPath -Encoding UTF8

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
