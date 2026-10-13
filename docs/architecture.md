# Architecture

## What this app is

A multi-tenant HR / company management API. One deployment serves every customer; the company context travels via a JWT claim on each request. MongoDB stores all data; tenant isolation is enforced inside the generic repository (see [`data-layer.md`](./data-layer.md)).

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 |
| Framework | ASP.NET Core (Minimal hosting) |
| Database | MongoDB (driver 3.3.0) |
| Auth | JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.11) + BCrypt for password hashing |
| Realtime | SignalR (`Microsoft.AspNetCore.SignalR` 1.2.0) — chat + notifications |
| Email | SendGrid (legacy MailKit also referenced) |
| PDF generation | PuppeteerSharp 20.2.2 (headless Chrome → HTML → PDF) for salary slips |
| Mapping | Mapster 13.0.1 |
| HTML sanitization | HtmlSanitizer (whitelist-based) |
| API docs | Swashbuckle / Swagger |
| LINQ helpers | LinqKit |

Pinning lives in each project's `.csproj`. The most-cited package CVEs (HtmlSanitizer 8.1.870, MailKit 4.11.0) should be bumped on the next maintenance pass.

## Solution layout 

The solution at [CodejiCMSCore.sln](../CodejiCMSCore.sln) has 6 projects:

```
CMS-Backend-Core/
├── Codeji.CMS.API/             # Web API: controllers, Program.cs, middleware, hubs
├── Codeji.CMS.Services/        # Business logic — orchestrates repositories, Mapster, background tasks
├── Codeji.CMS.Repository/      # MongoDB data access — generic repo, entities, Result<T>, registration
├── Codeji.CMS.DTO/             # Request/response models. Pure data — depends only on Utility
├── Codeji.CMS.Migrations/      # CLI executable: runs IMigration implementations against MongoDB
├── Codeji.CMS.Utility/         # Cross-cutting helpers: enums, constants, JWT/BCrypt helpers, sanitizer
└── docs/                       # You are here
```

Project dependency direction (one-way; no cycles):

```
API ──► Services ──► Repository ──► DTO ──► Utility
  │         │            │
  └─────────┴────────────┴───► (all reference Utility)
Migrations ──► Repository (only)
```

## Request lifecycle (authenticated request)

How a typical authenticated request flows:

1. **Client** sends `POST /api/user/GetAllEmployees` with `Authorization: Bearer <jwt>` and `XSRF-TOKEN` header.
2. **Middleware pipeline** ([Program.cs](../Codeji.CMS.API/Program.cs)):
   1. `ExceptionHandlingMiddleware` wraps the rest in a try/catch.
   2. Static-file / Swagger middleware (skipped for API routes).
   3. CORS check (whitelisted origins: `localhost`, `*.codeji.in`).
   4. `UseAuthentication` validates the JWT (issuer, audience, signature, lifetime).
   5. `UseAuthorization` checks `[Authorize]` / `[Authorize(Policy = "AdminOnly")]`.
   6. `AntiforgeryMiddleware` validates the XSRF token on state-changing verbs.
3. **Controller action** is matched. The action is decorated with `[ModulePermission("Employees", "View")]` from [`ModulePermissionAttribute.cs`](../Codeji.CMS.API/App_Start/ModulePermissionAttribute.cs). The middleware that enforces it ([`AuthenticateUserRequest.cs`](../Codeji.CMS.API/App_Start/AuthenticateUserRequest.cs)) reads the attribute, calls `IRoleService.VerifyUserAccess(...)`, and returns 403 on failure.
4. **Controller** validates `ModelState` (data-annotation rules on the DTO), then calls into the service layer. Controllers are thin — no business logic.
5. **Service** ([Codeji.CMS.Services/](../Codeji.CMS.Services/)) does the work: orchestrates repository calls, runs `Sanitizer.SanitizeProperties()` on rich-text fields, calls `_priorityTaskQueue.QueueBackgroundWorkItem(...)` for async side-effects (emails, notifications), maps via Mapster, and returns a `Result<T>`.
6. **Repository** ([Codeji.CMS.Repository/](../Codeji.CMS.Repository/)) is the generic `MongoRepository<T>`. It auto-injects `CompanyId`, `CreatedBy/UpdatedBy`, and `CreatedDate/UpdatedDate` via reflection. All reads are scoped to the current `CompanyId` (from JWT) and filter `IsDeleted = true`. See [`data-layer.md`](./data-layer.md).
7. **MongoDB** returns documents. Service maps to DTO, returns `Result<T>` to controller.
8. **Controller** returns the `Result<T>` to the client. Default JSON serialization wraps it.
9. **Background side effects** (email, SignalR notification) run asynchronously via [`PriorityTaskQueue`](../Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs) — they do not block the response.

The end-to-end shape is repeated in every controller — see [`conventions.md`](./conventions.md) for the canonical snippet.

## Project responsibilities

### Codeji.CMS.API
Controllers, `Program.cs`, middleware, SignalR hubs, mail templates, file upload directory.

| Folder | Purpose |
|---|---|
| [`Controllers/`](../Codeji.CMS.API/Controllers/) | 16 controllers, all `[Authorize]` except `AccountController` public endpoints |
| [`App_Start/`](../Codeji.CMS.API/App_Start/) | `ExceptionHandlingMiddleware`, `ModulePermissionAttribute`, `AuthenticateUserRequest`, `CustomAuthorizationPolicy`, `GetUserIdProvider`, `AntyForgeryMiddleware` |
| [`Notification/`](../Codeji.CMS.API/Notification/) | `NotificationHub` (SignalR), `NotificationService` |
| [`ChatHub/`](../Codeji.CMS.API/ChatHub/) | `ChatHub` (SignalR) |
| [`Templates/`](../Codeji.CMS.API/Templates/) | HTML email templates copied to output on build |
| `Uploads/` | User-uploaded files (profile pics, resumes); served via static-files at `/fs` (gitignored) |

### Codeji.CMS.Services
The business-logic layer. One folder per domain (`Account`, `Employees`, `LeaveManagement`, etc.), each with `XxxService.cs` + `Interface/IXxxService.cs`. DI registration is in [`ServicesRegistration.cs`](../Codeji.CMS.Services/Registration/ServicesRegistration.cs) (`AddBusinessServices()`); Mapster config is in [`MapsterConfig.cs`](../Codeji.CMS.Services/Registration/MapsterConfig.cs).

Notable subfolders: [`BackgroundTasks/`](../Codeji.CMS.Services/BackgroundTasks/) (priority queue + 3 hosted services), [`PayRoll/`](../Codeji.CMS.Services/PayRoll/) (includes `PdfService` and `AutoPayRollServices`), [`Companies/`](../Codeji.CMS.Services/Companies/) (includes `DefaultCompanySeeds`).

### Codeji.CMS.Repository
The data layer. Three things live here:

1. **Entities** ([`Entities/`](../Codeji.CMS.Repository/Entities/)) — MongoDB document classes, all inheriting [`BaseClass`](../Codeji.CMS.Repository/Entities/BaseClass.cs) (CompanyId, audit fields, IsDeleted).
2. **The generic repository** ([`Repositories/MongoRepository.cs`](../Codeji.CMS.Repository/Repositories/MongoRepository.cs) implementing [`IMongoDbRepository<T>`](../Codeji.CMS.Repository/Interfaces/IMongoDbRepository.cs)).
3. **Domain types** ([`Domain/`](../Codeji.CMS.Repository/Domain/)) — `Result`, `Result<T>`, `PagingInfo`, `PagingResult`.

DI registration is in [`RepositoryServicesRegistration.cs`](../Codeji.CMS.Repository/Registration/RepositoryServicesRegistration.cs).

### Codeji.CMS.DTO
Request and response models, organized by domain. **Pure data** — only references `Codeji.CMS.Utility` for enums and `MultilingualModel`. No service or repository dependencies.

### Codeji.CMS.Migrations
A CLI executable. [`Program.cs`](../Codeji.CMS.Migrations/Program.cs) loads MongoDB connection, [`MigrationLoader`](../Codeji.CMS.Migrations/MigrationLoader.cs) reflects on its own assembly to find every `IMigration`, [`MigrationRunner`](../Codeji.CMS.Migrations/MigrationRunner.cs) executes them in `Id`-string order.

Run with:
```
cd Codeji.CMS.Migrations && dotnet run
```

See [`migrations.md`](./migrations.md) for details.

### Codeji.CMS.Utility
Pure helpers, no other project references it inward. Contents:

| Folder / file | Purpose |
|---|---|
| [`Constraints/ConstraintHelper.cs`](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs) | `AppModule` constants, `Permission` constants, `Languages` constants, sanitizer whitelist |
| [`Enums/EnumsHelper.cs`](../Codeji.CMS.Utility/Enums/EnumsHelper.cs) | All app-wide enums (`Roles`, `MailType`, `LeaveTypes`, `ClaimTypesEnum`, …) |
| [`Helpers/AuthenticationHandler.cs`](../Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs) | BCrypt + JWT generation |
| [`Helpers/HtmlTemplate.cs`](../Codeji.CMS.Utility/Helpers/HtmlTemplate.cs) | `[Placeholder]`-based template renderer |
| [`Helpers/ConfigManager.cs`](../Codeji.CMS.Utility/Helpers/ConfigManager.cs) | Static singleton bound to `appsettings.json` |
| [`Helpers/CustomStatusCode.cs`](../Codeji.CMS.Utility/Helpers/CustomStatusCode.cs) | Custom status codes used in `Result<T>` |
| [`middlewares/CurrentContext.cs`](../Codeji.CMS.Utility/middlewares/CurrentContext.cs) | Static accessors for `UserId`, `CompanyId`, `RoleId` from JWT or `cId` header |
| [`Sanitizer.cs`](../Codeji.CMS.Utility/Sanitizer.cs) | `[Sanitize]` attribute + reflection-based property sanitization |

## Multi-tenancy in one paragraph

Every multi-tenant entity inherits `BaseClass`, which has `CompanyId`. On insert, `MongoRepository<T>` reads the current `CompanyId` from `CurrentContext.CompanyId(IHttpContextAccessor)` (JWT claim, or `cId` header for public routes like the career portal) and stamps it via reflection. On read, it appends `entity.CompanyId == GetCompanyId()` to every query. There is no cross-tenant query path through the repository — adding one would require explicit work and code review.

## Permission model

Three layers, all enforced server-side:

1. **`[Authorize]`** (or `[AllowAnonymous]`) on the controller — JWT must be valid.
2. **`[Authorize(Policy = "AdminOnly")]`** on admin-only endpoints — uses [`CustomAuthorizationPolicy.cs`](../Codeji.CMS.API/App_Start/CustomAuthorizationPolicy.cs) `RoleHandler` to check `roleType == Administrator`.
3. **`[ModulePermission(AppModule.X, Permission.Y)]`** — fine-grained. Enforced by [`AuthenticateUserRequest.cs`](../Codeji.CMS.API/App_Start/AuthenticateUserRequest.cs) which reads endpoint metadata and calls `IRoleService.VerifyUserAccess()`.

The permission table is `Module → ModulePermission → RolePermission → Roles`. Seeded by [`SeedBaseModulesAndPermissions.cs`](../Codeji.CMS.Migrations/Migrations/SeedBaseModulesAndPermissions.cs); per-company `Roles` and `RolePermission` rows are created during company registration in [`CompanyService.Register()`](../Codeji.CMS.Services/Companies/CompanyService.cs).

Detail in [`auth-and-permissions.md`](./auth-and-permissions.md).

## Build / run

| Command | What it does |
|---|---|
| `dotnet build` (from solution root) | Restores + compiles all 6 projects |
| `dotnet run` (in `Codeji.CMS.API/`) | Runs the API. Default ports: HTTP 5000, HTTPS 5001. Swagger at `/swagger` if `AppSettings.IsForDebug = true` |
| `dotnet run` (in `Codeji.CMS.Migrations/`) | Executes pending migrations against the configured MongoDB |
| `dotnet test` | (No test projects exist yet — adding `xunit` projects is a recommended next step) |

## Configuration

`appsettings.json` (gitignored — see [`.gitignore`](../.gitignore)) drives everything via [`ConfigManager`](../Codeji.CMS.Utility/Helpers/ConfigManager.cs). Sections:

| Section | Keys |
|---|---|
| `ConnectionStrings.mongodb` | MongoDB connection string with database name |
| `AppSettings` | `IsForDebug`, `AppVersion`, `APIUrl` (used as JWT issuer), `AppUrl` (used as JWT audience) |
| `Jwt` | `SecretKey` (HMAC-SHA256, ≥256-bit), `Expiry` (minutes) |
| `EmailSettings` | SendGrid API key, BCC, from address |
| `ReCaptcha` | Google reCAPTCHA v2/v3 secret |
| `FileSettings` | Upload directory, public file URL prefix |

`appsettings.Example.json` (commit-friendly) should be added to make onboarding easier; current convention is "ask a teammate."

## What lives where (cheat sheet)

| You're touching… | Open… |
|---|---|
| A controller / endpoint | [`Codeji.CMS.API/Controllers/`](../Codeji.CMS.API/Controllers/) |
| Business logic for a domain | [`Codeji.CMS.Services/<Domain>/`](../Codeji.CMS.Services/) |
| Adding a database collection / field | [`Codeji.CMS.Repository/Entities/<Domain>/`](../Codeji.CMS.Repository/Entities/) |
| Generic repository behavior | [`Codeji.CMS.Repository/Repositories/MongoRepository.cs`](../Codeji.CMS.Repository/Repositories/MongoRepository.cs) |
| Adding a request / response shape | [`Codeji.CMS.DTO/<Domain>/`](../Codeji.CMS.DTO/) |
| Mapster mapping | [`MapsterConfig.cs`](../Codeji.CMS.Services/Registration/MapsterConfig.cs) |
| Service DI registration | [`ServicesRegistration.cs`](../Codeji.CMS.Services/Registration/ServicesRegistration.cs) |
| Adding an enum | [`EnumsHelper.cs`](../Codeji.CMS.Utility/Enums/EnumsHelper.cs) |
| Adding a module / permission constant | [`ConstraintHelper.cs`](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs) |
| New email template | [`Codeji.CMS.API/Templates/`](../Codeji.CMS.API/Templates/) + a `MailType` enum value + a row in `MailTemplate` collection (via migration) |
| New background job | [`Codeji.CMS.Services/BackgroundTasks/`](../Codeji.CMS.Services/BackgroundTasks/) + register in `Program.cs` |
| New SignalR hub | New file in `Codeji.CMS.API/<HubName>/`, register `MapHub<T>` in `Program.cs` |
| New migration | [`Codeji.CMS.Migrations/Migrations/`](../Codeji.CMS.Migrations/Migrations/) — see [`migrations.md`](./migrations.md) |
