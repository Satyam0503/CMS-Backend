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

    [Parameter(Mandatory=$true)]
    [string]$EmailHost,

    [Parameter(Mandatory=$true)]
    [int]$EmailPort,

    [Parameter(Mandatory=$true)]
    [string]$EmailFromName,

    [Parameter(Mandatory=$true)]
    [string]$EmailFromEmail,

    [Parameter(Mandatory=$true)]
    [string]$EmailSecretKey,

    [Parameter(Mandatory=$false)]
    [string]$SendGridApiKey = "",

    [Parameter(Mandatory=$true)]
    [string]$ReCaptchaSecretKey,

    [Parameter(Mandatory=$true)]
    [string]$APIUrl,

    [Parameter(Mandatory=$true)]
    [string]$AppUrl
)

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
        Port = $EmailPort
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
}

$outputFile = Join-Path $OutputPath "appsettings.json"
$appsettings | ConvertTo-Json -Depth 10 | Out-File $outputFile -Encoding UTF8

Write-Host "appsettings.json generated successfully at: $outputFile"
