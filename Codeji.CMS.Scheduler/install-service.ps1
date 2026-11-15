# Installs or updates the Codeji CMS Scheduler Windows service. Run as Administrator from the publish folder.
param(
    [string]$ServiceName = "CodejiCmsScheduler",
    [string]$Account = "NT AUTHORITY\NetworkService"
)
$ErrorActionPreference = "Stop"
$exe = Join-Path $PSScriptRoot "Codeji.CMS.Scheduler.exe"

if (-not [System.Diagnostics.EventLog]::SourceExists($ServiceName)) {
    New-EventLog -LogName Application -Source $ServiceName
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -ne "Stopped") { Stop-Service $ServiceName -Force }
    sc.exe config $ServiceName binPath= "`"$exe`"" obj= $Account | Out-Null
} else {
    sc.exe create $ServiceName binPath= "`"$exe`"" start= delayed-auto obj= $Account DisplayName= "Codeji CMS Scheduler" | Out-Null
    sc.exe description $ServiceName "Runs Codeji CMS background jobs: payroll, leave accrual, reminders, outboxes." | Out-Null
    # Restart after 1 minute on failure, reset the failure count daily.
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
}

Start-Service $ServiceName
Get-Service $ServiceName
