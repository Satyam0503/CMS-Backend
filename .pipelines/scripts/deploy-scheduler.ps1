# Deploys the Codeji.CMS.Scheduler Windows service for one environment.
#   -Action Stop   : stop the service (before files are replaced and migrations run)
#   -Action Deploy : copy, configure and start it; disables it when no InternalApi key is configured,
#                    in which case the API keeps running background jobs (BackgroundJobs:RunInApi=true).
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Stop", "Deploy")]
    [string]$Action,

    [Parameter(Mandatory=$true)]
    [string]$ServiceName,

    [string]$SourcePath,
    [string]$InstallPath,
    [string]$ApiPath,
    [string]$ApiUrl,

    [AllowEmptyString()]
    [string]$InternalApiKey = ""
)
$ErrorActionPreference = "Stop"
# An empty pipeline variable can arrive as its literal macro text.
if ($InternalApiKey -like '$(*') { $InternalApiKey = "" }
$account = "NT AUTHORITY\NetworkService"
$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($Action -eq "Stop") {
    if ($svc -and $svc.Status -ne "Stopped") {
        Write-Host "Stopping $ServiceName"
        Stop-Service $ServiceName -Force
        $svc.WaitForStatus("Stopped", [TimeSpan]::FromMinutes(2))
    }
    return
}

if ([string]::IsNullOrWhiteSpace($InternalApiKey)) {
    if ($svc) {
        sc.exe config $ServiceName start= disabled | Out-Null
        Write-Host "InternalApi.Key not set: $ServiceName disabled, API runs background jobs."
    } else {
        Write-Host "InternalApi.Key not set: scheduler not installed, API runs background jobs."
    }
    return
}

foreach ($name in @("SourcePath", "InstallPath", "ApiPath", "ApiUrl")) {
    if ([string]::IsNullOrWhiteSpace((Get-Variable $name -ValueOnly))) { throw "-$name is required for Deploy." }
}
if ($ApiUrl -notmatch '^(https?://[^/\\?#]+)') { throw "Could not parse origin from ApiUrl: '$ApiUrl'" }
$apiOrigin = $matches[1]

Write-Host "=========================================="
Write-Host "Deploying scheduler $ServiceName"
Write-Host "   Path:     $InstallPath"
Write-Host "   API root: $ApiPath"
Write-Host "   Relay:    $apiOrigin"
Write-Host "=========================================="

# Clean copy so DLLs from earlier releases don't linger. Nothing stateful lives here:
# uploads and evidence are written under the API folder.
if (Test-Path $InstallPath) { Remove-Item -Path $InstallPath -Recurse -Force }
robocopy $SourcePath $InstallPath /E /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -ge 8) { throw "robocopy (scheduler) failed with exit code $LASTEXITCODE" }
$global:LASTEXITCODE = 0

$settings = @{
    Scheduler = @{ ApiContentRoot = $ApiPath }
    InternalApi = @{ BaseUrl = $apiOrigin; Key = $InternalApiKey }
    Logging = @{
        LogLevel = @{ Default = "Information"; "Microsoft.Hosting.Lifetime" = "Information" }
        EventLog = @{ SourceName = $ServiceName; LogLevel = @{ Default = "Warning" } }
    }
}
$settings | ConvertTo-Json -Depth 10 | Out-File (Join-Path $InstallPath "appsettings.json") -Encoding UTF8

function Grant([string]$path, [string]$rights) {
    icacls $path /grant "${account}:$rights" /Q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "icacls failed on $path (exit $LASTEXITCODE)" }
}

# The service runs inside the API folder: it reads appsettings/Templates and writes payslips and evidence there.
Grant $ApiPath "(OI)(CI)RX"
foreach ($d in @("Uploads", "App_Data", "PrivateLeaveEvidence", "PrivateLifecycleEvidence")) {
    $p = Join-Path $ApiPath $d
    if (-not (Test-Path $p)) { New-Item -Path $p -ItemType Directory -Force | Out-Null }
    Grant $p "(OI)(CI)M"
}
Grant $InstallPath "(OI)(CI)RX"

& (Join-Path $InstallPath "install-service.ps1") -ServiceName $ServiceName -Account $account
