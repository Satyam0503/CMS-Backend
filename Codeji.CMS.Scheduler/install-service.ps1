# Installs or updates the Codeji CMS Scheduler Windows service and starts it. Run as Administrator from the publish folder.
param(
    [string]$ServiceName = "CodejiCmsScheduler",
    [string]$Account = "NT AUTHORITY\NetworkService"
)
$ErrorActionPreference = "Stop"
$exe = Join-Path $PSScriptRoot "Codeji.CMS.Scheduler.exe"
if (-not (Test-Path $exe)) { throw "Codeji.CMS.Scheduler.exe not found in $PSScriptRoot" }

if (-not [System.Diagnostics.EventLog]::SourceExists($ServiceName)) {
    New-EventLog -LogName Application -Source $ServiceName
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -ne "Stopped") {
        Stop-Service $ServiceName -Force
        $existing.WaitForStatus("Stopped", [TimeSpan]::FromMinutes(1))
    }
    sc.exe config $ServiceName binPath= "`"$exe`"" start= delayed-auto | Out-Null
} else {
    sc.exe create $ServiceName binPath= "`"$exe`"" start= delayed-auto DisplayName= "Codeji CMS Scheduler ($ServiceName)" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sc.exe create failed with exit code $LASTEXITCODE" }
    sc.exe description $ServiceName "Runs Codeji CMS background jobs: payroll, leave accrual, reminders, outboxes." | Out-Null
    # Restart after 1 minute on failure; reset the failure count daily.
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
}
if ($LASTEXITCODE -ne 0) { throw "sc.exe failed with exit code $LASTEXITCODE" }
$global:LASTEXITCODE = 0

# Set the account through WMI: sc.exe needs an empty password= argument, which PowerShell drops.
$result = Get-CimInstance Win32_Service -Filter "Name='$ServiceName'" |
    Invoke-CimMethod -MethodName Change -Arguments @{ StartName = $Account; StartPassword = "" }
if ($result.ReturnValue -ne 0) { throw "Setting service account failed (Win32_Service.Change returned $($result.ReturnValue))" }

Start-Service $ServiceName
(Get-Service $ServiceName).WaitForStatus("Running", [TimeSpan]::FromMinutes(1))
Get-Service $ServiceName
