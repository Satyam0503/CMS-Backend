# Codeji CMS

Codeji CMS is a multi-tenant HRMS and public careers platform. This repository contains the ASP.NET Core API, MongoDB persistence layer, services, migrations, and backend tests. Its web client is the sibling `../CMS-React` repository.

## Prerequisites

- .NET SDK 10.0.x (`dotnet --info`)
- MongoDB 6+; use a replica set for production and transaction-dependent workflows
- Node.js 20.x and npm for the frontend

Never commit MongoDB, JWT, SMTP, reCAPTCHA, or third-party API secrets. Override them through user-secrets, environment variables, or the deployment secret store.

## Configuration

```powershell
$env:ConnectionStrings__mongodb = 'mongodb://localhost:27017/codejiCms'
$env:Jwt__SecretKey = 'a-long-random-secret'
$env:EmailSettings__Host = 'smtp.example.com'
$env:EmailSettings__FromEmail = 'noreply@example.com'
$env:reCaptcha__SecretKey = '...'
```

For reverse-proxy deployments, configure only direct proxy IPs. The API ignores forwarded client headers from all other sources:

```json
"ReverseProxy": { "ForwardLimit": 1, "TrustedProxies": [ "10.0.0.10" ] }
```

The proxy must send standard `X-Forwarded-For`, `X-Forwarded-Host`, and `X-Forwarded-Proto` headers. Do not trust public client networks.

## Database initialization and backfills

1. Start MongoDB and set `ConnectionStrings__mongodb`.
2. Restore and build the solution.
3. Run the migrations project using environment-specific configuration.
4. Run matching backfills only after their schema/code is deployed (current examples: career-profile themes and employee employment status).
5. Verify tenant identifiers, indexes, and public-career data before enabling traffic.

Back up the database before every migration/backfill. Run production migrations once using a controlled deployment identity.

## Local development

From this repository:

```powershell
dotnet restore CodejiCMSCore.sln
dotnet run --project Codeji.CMS.API
```

The API normally runs at `http://localhost:5036` (Swagger: `http://localhost:5036/swagger`).

From the sibling frontend repository:

```powershell
cd ..\CMS-React
npm ci
npm run dev
```

The Vite client normally runs at `http://localhost:5173`; point its API configuration at the local backend.

## Tests and checks

```powershell
dotnet test Codeji.CMS.Services.Tests/Codeji.CMS.Services.Tests.csproj
dotnet test Codeji.CMS.API.Tests/Codeji.CMS.API.Tests.csproj
```

From `../CMS-React`:

```powershell
npm run typecheck
npm run lint
npm run test
npm run build
```

Browser tests require a deliberately selected target, running services, and valid non-production test credentials.

## Deployment prerequisites

- Keep secrets in the deployment platform, not repository files.
- Use TLS and configure deployed CORS origins.
- Configure the explicit reverse-proxy allow-list before relying on IP throttling.
- Use a MongoDB replica set; verify backups and restore procedures.
- Run migrations/backfills before serving the new version.
- Run backend/API and frontend checks, then verify public careers and tenant-scoped HR flows with dedicated test accounts.
