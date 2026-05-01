param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName,

    [Parameter(Mandatory=$true)]
    [string]$PhysicalPath,

    [Parameter(Mandatory=$true)]
    [string]$SourcePath,

    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = $SiteName
)

Import-Module WebAdministration

Write-Host "Starting deployment to IIS..."
Write-Host "Site Name: $SiteName"
Write-Host "Physical Path: $PhysicalPath"
Write-Host "Source Path: $SourcePath"

# Check if Application Pool exists
$appPoolExists = Test-Path "IIS:\AppPools\$AppPoolName"
if (-not $appPoolExists) {
    Write-Host "Creating Application Pool: $AppPoolName"
    New-WebAppPool -Name $AppPoolName
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name enable32BitAppOnWin64 -Value $false
}

# Stop Application Pool
Write-Host "Stopping Application Pool: $AppPoolName"
Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5

# Create Physical Directory if it doesn't exist
if (-not (Test-Path $PhysicalPath)) {
    Write-Host "Creating physical directory: $PhysicalPath"
    New-Item -Path $PhysicalPath -ItemType Directory -Force
}

# Copy files
Write-Host "Copying files from $SourcePath to $PhysicalPath"
Copy-Item -Path "$SourcePath\*" -Destination $PhysicalPath -Recurse -Force

# Check if Website exists
$siteExists = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $siteExists) {
    Write-Host "Creating IIS Website: $SiteName"
    New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName
}

# Start Application Pool
Write-Host "Starting Application Pool: $AppPoolName"
Start-WebAppPool -Name $AppPoolName

Write-Host "Deployment completed successfully!"
