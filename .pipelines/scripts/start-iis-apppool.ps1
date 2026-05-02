# Start IIS Application Pool (idempotent - skips if already started or missing)
# Usage: .\start-iis-apppool.ps1 -AppPoolName "crm-test-api"

param(
    [Parameter(Mandatory=$true)]
    [string]$AppPoolName
)

Import-Module WebAdministration

$pool = Get-Item "IIS:\AppPools\$AppPoolName" -ErrorAction SilentlyContinue
if (-not $pool) {
    Write-Host "App Pool '$AppPoolName' does not exist - nothing to start"
    return
}

if ($pool.State -eq "Started") {
    Write-Host "App Pool '$AppPoolName' is already running"
    return
}

Write-Host "Starting Application Pool: $AppPoolName"
Start-WebAppPool -Name $AppPoolName
Write-Host "Application Pool started successfully"
