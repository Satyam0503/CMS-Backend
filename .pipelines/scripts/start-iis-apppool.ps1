# Start IIS Application Pool
# Usage: .\start-iis-apppool.ps1 -AppPoolName "codeji.dev.api"

param(
    [Parameter(Mandatory=$true)]
    [string]$AppPoolName
)

Import-Module WebAdministration

Write-Host "▶️  Starting Application Pool: $AppPoolName"
Start-WebAppPool -Name $AppPoolName
Write-Host "✅ Application Pool started successfully"
