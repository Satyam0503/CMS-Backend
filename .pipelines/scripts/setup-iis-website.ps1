# Setup IIS Website and Application Pool for .NET Core
# Usage: .\setup-iis-website.ps1 -SiteName "crm-test-api" -PhysicalPath "D:\Hosting\Codeji\crm-test-api" -HostName "testapi.codeji.in"

param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName,

    [Parameter(Mandatory=$true)]
    [string]$PhysicalPath,

    [Parameter(Mandatory=$false)]
    [int]$Port = 80,

    [Parameter(Mandatory=$false)]
    [string]$HostName = "",

    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = ""
)

if ([string]::IsNullOrWhiteSpace($AppPoolName)) {
    $AppPoolName = $SiteName
}

Write-Host "=========================================="
Write-Host "Setting up IIS Website: $SiteName"
Write-Host "  Pool:    $AppPoolName"
Write-Host "  Path:    $PhysicalPath"
Write-Host "  Binding: *:${Port}:${HostName}"
Write-Host "=========================================="

# Import WebAdministration module
Import-Module WebAdministration

# Create Physical Directory if it doesn't exist
if (-not (Test-Path $PhysicalPath)) {
    Write-Host "Creating physical directory: $PhysicalPath"
    New-Item -Path $PhysicalPath -ItemType Directory -Force | Out-Null
    Write-Host "Directory created"
} else {
    Write-Host "Physical directory already exists"
}

# Check if Application Pool exists, create if not
$appPool = Get-Item "IIS:\AppPools\$AppPoolName" -ErrorAction SilentlyContinue
if (-not $appPool) {
    Write-Host "Creating Application Pool: $AppPoolName"
    New-WebAppPool -Name $AppPoolName | Out-Null

    # No Managed Code for .NET Core / .NET 8
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name enable32BitAppOnWin64 -Value $false
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"

    Write-Host "Application Pool created and configured for .NET Core"
} else {
    Write-Host "Application Pool already exists"
}

# Stop application pool before deployment (release file locks)
Write-Host "Stopping Application Pool: $AppPoolName"
Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Write-Host "Application Pool stopped"

# Check if Website exists, create if not
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $site) {
    Write-Host "Creating IIS Website: $SiteName"
    if ([string]::IsNullOrWhiteSpace($HostName)) {
        New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName -Port $Port | Out-Null
    } else {
        New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName -Port $Port -HostHeader $HostName | Out-Null
    }
    Write-Host "IIS Website created successfully"
} else {
    Write-Host "IIS Website already exists - reconciling pool and bindings"

    # Ensure correct app pool
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name "applicationPool" -Value $AppPoolName

    # Ensure expected binding exists
    $expectedBinding = if ([string]::IsNullOrWhiteSpace($HostName)) { "*:${Port}:" } else { "*:${Port}:${HostName}" }
    $bindings = Get-WebBinding -Name $SiteName -ErrorAction SilentlyContinue
    $hasBinding = $bindings | Where-Object { $_.bindingInformation -eq $expectedBinding -and $_.protocol -eq "http" }
    if (-not $hasBinding) {
        Write-Host "Adding missing binding: $expectedBinding"
        if ([string]::IsNullOrWhiteSpace($HostName)) {
            New-WebBinding -Name $SiteName -Protocol "http" -Port $Port | Out-Null
        } else {
            New-WebBinding -Name $SiteName -Protocol "http" -Port $Port -HostHeader $HostName | Out-Null
        }
    }
}

Write-Host "=========================================="
Write-Host "IIS configuration completed"
Write-Host "=========================================="
