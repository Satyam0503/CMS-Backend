# Setup IIS Website and Application Pool for .NET Core
# Usage: .\setup-iis-website.ps1 -SiteName "codeji.dev.api" -PhysicalPath "C:\inetpub\wwwroot\app" -Port 80

param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName,

    [Parameter(Mandatory=$true)]
    [string]$PhysicalPath,

    [Parameter(Mandatory=$true)]
    [int]$Port
)

Write-Host "=========================================="
Write-Host "Setting up IIS Website: $SiteName"
Write-Host "=========================================="

# Import WebAdministration module
Import-Module WebAdministration

# Create Physical Directory if it doesn't exist
if (-not (Test-Path $PhysicalPath)) {
    Write-Host "📁 Creating physical directory: $PhysicalPath"
    New-Item -Path $PhysicalPath -ItemType Directory -Force | Out-Null
    Write-Host "✅ Directory created"
} else {
    Write-Host "✅ Physical directory already exists"
}

# Check if Application Pool exists, create if not
$appPool = Get-Item "IIS:\AppPools\$SiteName" -ErrorAction SilentlyContinue
if (-not $appPool) {
    Write-Host "🔧 Creating Application Pool: $SiteName"
    New-WebAppPool -Name $SiteName | Out-Null

    # Set to No Managed Code for .NET Core
    Set-ItemProperty "IIS:\AppPools\$SiteName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$SiteName" -Name enable32BitAppOnWin64 -Value $false

    Write-Host "✅ Application Pool created and configured for .NET Core"
} else {
    Write-Host "✅ Application Pool already exists"
}

# Stop application pool before deployment
Write-Host "⏸️  Stopping Application Pool: $SiteName"
Stop-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Write-Host "✅ Application Pool stopped"

# Check if Website exists, create if not
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $site) {
    Write-Host "🌐 Creating IIS Website: $SiteName on port $Port"
    New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $SiteName -Port $Port | Out-Null
    Write-Host "✅ IIS Website created successfully"
} else {
    Write-Host "✅ IIS Website already exists"
}

Write-Host "=========================================="
Write-Host "✅ IIS configuration completed"
Write-Host "=========================================="
