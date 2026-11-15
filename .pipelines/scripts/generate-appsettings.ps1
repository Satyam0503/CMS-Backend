# Generate appsettings.{Environment}.json
# Usage: .\generate-appsettings.ps1 -Environment "Development" -OutputPath "C:\path\to\output" -BuildNumber "1.0.0" [other parameters...]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Development", "Production")]
    [string]$Environment,

    [Parameter(Mandatory=$true)]
    [string]$OutputPath,

    [Parameter(Mandatory=$true)]
    [string]$BuildNumber,

    [Parameter(Mandatory=$true)]
    [string]$IsForDebug,

    [Parameter(Mandatory=$true)]
    [string]$MongoConnectionString,

    [Parameter(Mandatory=$true)]
    [string]$JwtSecretKey,

    [Parameter(Mandatory=$true)]
    [string]$JwtExpiry,

    [Parameter(Mandatory=$false)]
    [AllowEmptyString()]
    [string]$EmailHost = "",

    [Parameter(Mandatory=$false)]
    [string]$EmailPort = "0",

    [Parameter(Mandatory=$true)]
    [string]$EmailFromName,

    [Parameter(Mandatory=$true)]
    [string]$EmailFromEmail,

    [Parameter(Mandatory=$false)]
    [AllowEmptyString()]
    [string]$EmailSecretKey = "",

    [Parameter(Mandatory=$false)]
    [AllowEmptyString()]
    [string]$SendGridApiKey = "",

    [Parameter(Mandatory=$true)]
    [string]$ReCaptchaSecretKey,

    [Parameter(Mandatory=$true)]
    [string]$APIUrl,

    [Parameter(Mandatory=$true)]
    [string]$AppUrl,

    # Set => Codeji.CMS.Scheduler owns background jobs and the API only relays its notifications.
    [Parameter(Mandatory=$false)]
    [AllowEmptyString()]
    [string]$InternalApiKey = ""
)

# An empty pipeline variable can arrive as its literal macro text.
if ($InternalApiKey -like '$(*') { $InternalApiKey = "" }

Write-Host "Generating appsettings.json"
Write-Host "Output Path: $OutputPath"

$logLevel = if ($Environment -eq "Development") { "Information" } else { "Warning" }
$aspNetLogLevel = if ($Environment -eq "Development") { "Warning" } else { "Error" }

$appsettings = @{
    ConnectionStrings = @{
        mongodb = $MongoConnectionString
    }
    AppSettings = @{
        isForDebug = $IsForDebug
        appVersion = $BuildNumber
        APIUrl = $APIUrl
        AppUrl = $AppUrl
    }
    FileSettings = @{
        UploadUrl = "uploads/"
        Employee_ImageUrl = "fs/ProfileImage/"
        viewResumeUrl = "fs/Resume/"
        CompanyLogoUrl = "fs/CompanyLogo/"
        CalendarItemCoverImage = "fs/CalendarItemCoverPictures/"
        PolicyDocument = "fs/Policy/"
    }
    EmailSettings = @{
        Host = $EmailHost
        Port = if ([string]::IsNullOrWhiteSpace($EmailPort)) { 0 } else { [int]$EmailPort }
        FromName = $EmailFromName
        FromEmail = $EmailFromEmail
        BccEmail = "hello@codeji.in"
        SupportEmail = "hr@codeji.in"
        SecretKey = $EmailSecretKey
        SendGridApiKey = $SendGridApiKey
    }
    reCaptcha = @{
        SecretKey = $ReCaptchaSecretKey
    }
    Jwt = @{
        SecretKey = $JwtSecretKey
        Expiry = $JwtExpiry
    }
    Logging = @{
        LogLevel = @{
            Default = $logLevel
            "Microsoft.AspNetCore" = $aspNetLogLevel
        }
    }
    AllowedHosts = "*"
    BackgroundJobs = @{
        RunInApi = [string]::IsNullOrWhiteSpace($InternalApiKey)
    }
    InternalApi = @{
        Key = $InternalApiKey
    }
}

$outputFile = Join-Path $OutputPath "appsettings.json"
$appsettings | ConvertTo-Json -Depth 10 | Out-File $outputFile -Encoding UTF8

Write-Host "appsettings.json generated successfully at: $outputFile"
