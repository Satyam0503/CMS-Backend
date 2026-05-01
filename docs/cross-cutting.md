# Cross-Cutting Infrastructure

The bits that don't belong to a single business module: middleware, hosted services, SignalR hubs, mail, sanitization, and logging.

---

## Middleware pipeline

Configured in [Program.cs](../Codeji.CMS.API/Program.cs) — search for `app.Use…`. Order matters; this is the canonical sequence:

1. **`ExceptionHandlingMiddleware`** — global try/catch.
2. **Static files** + **Swagger** (if `IsForDebug`).
3. **CORS** — `UseCors("codeji")`.
4. **`UseAuthentication`** — JWT bearer.
5. **`UseAuthorization`** — `[Authorize]` + policies.
6. **`AntyForgeryMiddleware`** — XSRF on state-changing verbs.
7. **`AuthenticateUserRequest`** — module/permission attribute enforcement (yes, after authorization — runs as part of routing, not as MVC filter).
8. **Security-headers middleware** (inline lambda) — HSTS, nosniff, frame-deny, CSP.
9. **`UseForwardedHeaders`** — for reverse proxy (X-Forwarded-For/Host).
10. **`MapControllers()`**, **`MapHub<NotificationHub>("/notificationhub")`**, **`MapHub<ChatHub>("/chathub")`**.

### ExceptionHandlingMiddleware

[`ExceptionHandlingMiddleware.cs`](../Codeji.CMS.API/App_Start/ExceptionHandlingMiddleware.cs)

- Catches everything.
- Special-cases `AntiforgeryValidationException` → returns status 512 with the original message.
- In debug mode (`AppSettings.IsForDebug == true`): re-throws — full stack trace returned to the client.
- In production: logs the error, returns a generic JSON 500 (`{ success: false, statusCode: 500, message: "An unexpected error occurred." }`).

> **Caveat.** Re-throwing in debug mode should never be enabled in a real environment. Treat `IsForDebug` as a development-only flag.

### AntyForgeryMiddleware

[`AntyForgeryMiddleware.cs`](../Codeji.CMS.API/App_Start/AntyForgeryMiddleware.cs)

Wraps ASP.NET Core's built-in antiforgery. The frontend pulls a token pair via `GET api/account/antiforgerytoken/{appKey}`, stores both, and echoes the request token in the `XSRF-TOKEN` header on POST/PUT/DELETE/PATCH. Cookie name is `X_CSRFTken` (typo preserved for compatibility).

### AuthenticateUserRequest

[`AuthenticateUserRequest.cs`](../Codeji.CMS.API/App_Start/AuthenticateUserRequest.cs)

Reads the `[ModulePermission(...)]` attribute on the matched endpoint and calls `IRoleService.VerifyUserAccess(...)`. Returns 403 on failure. This is the bridge between the simple attribute and the runtime check — see [`auth-and-permissions.md`](./auth-and-permissions.md).

### Security-headers middleware

Inline lambda in `Program.cs`. Adds:

```
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload;
referrer-policy: same-origin
x-content-type-options: nosniff
x-frame-options: DENY
X-Permitted-Cross-Domain-Policies: none
x-xss-protection: 1; mode=block
Content-Security-Policy: default-src 'self';
```

…and removes `Server` and `X-Powered-By`. The CSP is restrictive — if you ever serve inline scripts from the API (you don't, currently), you'd need to relax it.

> **Caveat.** Multiple `Headers.Add(...)` calls in the same lambda will throw if a header already exists. The compiler analyzer flags `ASP0019` warnings on `Program.cs` lines ~304-307. Switch to `Headers.Append(...)` or the indexer when you next touch this file.

---

## SignalR hubs

Two hubs, both WebSocket-only:

| Hub | Path | File | Purpose |
|---|---|---|---|
| `NotificationHub` | `/notificationhub` | [Notification/NotificationHub.cs](../Codeji.CMS.API/Notification/NotificationHub.cs) | Server → client notifications (leave status, holiday reminder, birthday wishes, etc.) |
| `ChatHub` | `/chathub` | [ChatHub/ChatHub.cs](../Codeji.CMS.API/ChatHub/ChatHub.cs) | Real-time chat between employees |

Configured in `Program.cs`:

```csharp
app.MapHub<NotificationHub>("/notificationhub", o =>
{
    o.Transports = HttpTransportType.WebSockets;
    o.ApplicationMaxBufferSize = 6_000_000;
    o.TransportMaxBufferSize  = 6_000_000;
});
```

User identity (so `Clients.User(userId)` works) comes from [`GetUserIdProvider.cs`](../Codeji.CMS.API/App_Start/GetUserIdProvider.cs) which reads the `sub` claim from the JWT — see [`auth-and-permissions.md`](./auth-and-permissions.md) for how the JWT travels in the WebSocket query string.

### Sending notifications from a service

Inject `IHubContext<NotificationHub>` (or use [`NotificationService`](../Codeji.CMS.API/Notification/NotificationService.cs) which wraps it). Don't hold a reference to the `Hub` instance — they're transient and disposed per connection.

---

## Background tasks

Three hosted services run continuously, plus a priority queue used for fire-and-forget work from request handlers.

### IPriorityTaskQueue + PriorityQueuedHostedService

| File | Role |
|---|---|
| [`PriorityTaskQueue.cs`](../Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs) | Interface + implementation: `QueueBackgroundWorkItem(Func<CancellationToken, Task>, int priority)` |
| [`PriorityQueuedHostedService.cs`](../Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs) | `BackgroundService` that dequeues + executes |

Use it from any service that needs to do work after responding (sending an email, broadcasting a notification, generating a PDF for an audit log):

```csharp
_priorityTaskQueue.QueueBackgroundWorkItem(async ct =>
{
    await _emailService.Send(...);
}, priority: 1);
return result;   // request returns immediately
```

Higher priority = runs first. There's a single worker — long-running items block the queue. For genuinely heavy work, spawn a new task or push to an external queue (Service Bus, Azure Functions).

### LeaveAccrualHostedService

[`LeaveAccrualHostedService.cs`](../Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs)

- Runs as a `BackgroundService` (started at app start, stops on shutdown).
- Schedules itself for the **1st of each month** (UTC).
- Resolves `ILeaveManagementService` via `_serviceProvider.CreateScope()` (because the hosted service is singleton and the business service is scoped).
- Iterates leave policies and stamps fresh balances on each `EmployeeLeaveBalance` row.

Logs:
```
Running leave accrual service on the 1st of the month: 2026-05-01T00:00:00Z
Leave accrual service Next run scheduled for: 2026-06-01T00:00:00Z
```

### BirthdayAndAnniversaryNotificationHostedServices

[`BirthdayAndAnniversaryNotificationHostedServices.cs`](../Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs)

- Runs once per day.
- Queries each company for employees with today's birthday or work anniversary (respecting `NotificationPreference`).
- Pushes a notification via `NotificationHub` and queues an email via `IPriorityTaskQueue`.

### PayRollHostedService

[`PayRollHostedServices.cs`](../Codeji.CMS.Services/PayRoll/PayRollHostedServices.cs)

- Scheduled monthly payroll generation.
- Iterates active employees per company, calls `SalaryCalculator`, writes `EmpPayRoll` rows, generates PDFs via `PdfService`.

### Adding a new background job

1. Create a class deriving `BackgroundService` (in [`Codeji.CMS.Services/BackgroundTasks/`](../Codeji.CMS.Services/BackgroundTasks/)).
2. Override `ExecuteAsync(CancellationToken stoppingToken)`. Use `Task.Delay` to schedule, **don't poll** in a tight loop.
3. To use scoped services, inject `IServiceProvider` and `using var scope = _serviceProvider.CreateScope(); var svc = scope.ServiceProvider.GetRequiredService<IThing>();` inside the method.
4. Register in `Program.cs`: `builder.Services.AddHostedService<MyJob>();`
5. Inject `ILogger<MyJob>` and log every scheduled run + every error — these are easy to lose otherwise.

---

## HTML sanitization

[`Sanitizer.cs`](../Codeji.CMS.Utility/Sanitizer.cs)

Used for any DTO field that holds user-authored HTML (notice body, policy body, job description). Whitelist:

| Allowed tags | `p`, `strong`, `em`, `u`, `i`, `s`, `h1`–`h6`, `ol`, `ul`, `li`, `span`, `br` |
| Allowed attributes | `style`, `class` |

(Defined in [`ConstraintHelper.cs`](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs).)

### Two ways to use it

**Per-property attribute** (preferred for DTOs):

```csharp
public class AddNoticeRequest
{
    public string Title { get; set; }

    [Sanitize]
    public string Body { get; set; }   // HTML
}
```

…then in the service, **before save**:

```csharp
Sanitizer.SanitizeProperties(request);
```

This walks the DTO via reflection and sanitizes every `[Sanitize]`-decorated `string` or `List<string>`.

**Direct call** (for one-off cases):

```csharp
var safe = Sanitizer.EncodingHtmlText(rawHtml);
```

> **Caveat.** [`Sanitizer.cs:31`](../Codeji.CMS.Utility/Sanitizer.cs#L31) has an empty `catch (Exception ex) { }`. If sanitization throws, you save the *unsanitized* string. Replace with a logged catch or a rethrow when convenient.

---

## Mail

### Sending

[`EmailService.cs`](../Codeji.CMS.Services/Registration/EmailService.cs) (interface `IEmailService`).

- Pulls SendGrid via `AddSendGrid()` in `Program.cs` using `EmailSettings.SendGrid.ApiKey` from `ConfigManager`.
- Loads HTML body from the `MailTemplate` MongoDB collection (matched by `MailType` enum) — not from disk.
- Template files in [`Codeji.CMS.API/Templates/`](../Codeji.CMS.API/Templates/) are for *file-based* templates (e.g. salary-slip HTML) — **not** for the mail templates which are DB-stored and editable per company.
- Renders via `HtmlTemplate.Render(template, valuesObject)`.
- Sends via SendGrid; logs the send to the `EmpEmailLogs` collection.
- Always called from inside `_priorityTaskQueue.QueueBackgroundWorkItem(...)` so the user request returns immediately.

### Templates

Stored in the `MailTemplate` collection — one row per `MailType` enum value. Seeded by [`SeedMailTemplates.cs`](../Codeji.CMS.Migrations/Migrations/SeedMailTemplates.cs) at first run.

To customize a template per company, the long-term plan is per-company override rows; for now the template is global.

### Placeholder syntax

`HtmlTemplate.Render(template, values)` — see [`HtmlTemplate.cs`](../Codeji.CMS.Utility/Helpers/HtmlTemplate.cs):

```csharp
public static string Render(string htmlTemplate, object values)
{
    var output = htmlTemplate;
    foreach (var p in values.GetType().GetProperties())
    {
        var val = p.GetValue(values, null);
        output = output.Replace("[" + p.Name + "]", val?.ToString() ?? "");
    }
    return output;
}
```

So a property `EmployeeName = "Jay"` replaces `[EmployeeName]` in the template. **Reflection-based**, **case-sensitive**, **no escaping** (the template author is trusted).

When you add a placeholder to a template, also add the property to the anonymous values object you pass to `Render()` — silent fall-through happens otherwise (template renders with literal `[NewPlaceholder]`).

### Adding a new mail type

1. Add a value to `EnumsHelper.MailType` in [`EnumsHelper.cs`](../Codeji.CMS.Utility/Enums/EnumsHelper.cs).
2. Add a row to `MailTemplate` in a new migration (mirror the structure in [`SeedMailTemplates.cs`](../Codeji.CMS.Migrations/Migrations/SeedMailTemplates.cs)).
3. Send it from a service via `EmailService.Send(MailType.MyNew, …)`.

---

## PDF generation

[`PdfService.cs`](../Codeji.CMS.Services/PdfService.cs) — uses **PuppeteerSharp** (headless Chrome).

- On first call, downloads the bundled Chromium build (~150MB) into the OS user cache. Slow first hit — pre-warm in dev.
- Renders an HTML template (loaded from [`Codeji.CMS.API/Templates/`](../Codeji.CMS.API/Templates/)) via `HtmlTemplate.Render`, opens it in headless Chrome, prints to PDF.
- Returns the PDF as a `byte[]`.

Used by `PayRollServices.GenerateSalarySlip`. If you add another PDF, follow the same shape — keep templates in `Templates/` (set `CopyToOutputDirectory` so they ship in the build).

> **Performance note.** PuppeteerSharp opens and closes a Chromium process per render. For high-volume PDF generation you'd reuse a browser instance — not currently a bottleneck.

---

## Configuration

[`ConfigManager.cs`](../Codeji.CMS.Utility/Helpers/ConfigManager.cs) — static singleton initialized once at startup from `appsettings.json`. Sections exposed:

| Property | What |
|---|---|
| `AppSettings` | `IsForDebug`, `AppVersion`, `APIUrl`, `AppUrl` |
| `Jwt` | `SecretKey`, `Expiry` (minutes), `RefreshTokenExpiry` (days) |
| `EmailSettings` | SendGrid API key, from address, BCC, support address |
| `ReCaptcha` | Secret key |
| `FileSettings` | Upload directory, public URL prefix, image/resume/logo paths |

Anywhere in the codebase you see `ConfigManager.AppSettings.APIUrl` or similar — that's the static read.

> **Caveat.** Changing `Jwt.SecretKey` invalidates every existing token. Plan rotations.

---

## Logging

`Microsoft.Extensions.Logging` only — no Serilog, no Application Insights wired up at the framework level.

Pattern:

```csharp
public class LeaveAccrualHostedService(
    ILogger<LeaveAccrualHostedService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<LeaveAccrualHostedService> _logger = logger;
    // …
    _logger.LogInformation("Running leave accrual service on the 1st of the month: {Now}", now);
    _logger.LogError(ex, "Failed to accrue leave for company {CompanyId}", companyId);
}
```

Use **structured** logging (`{Property}` placeholders, not string interpolation) so the log analyzer can index. The hosted services already do this; new code should match.

Output goes to the console by default. In production, configure a sink (App Insights, Seq, ELK) via `appsettings.json` `Logging` section — it isn't done yet, so prod logs only live in the container's stdout.

---

## File uploads

| What | Where |
|---|---|
| Upload directory | `Codeji.CMS.API/Uploads/` (gitignored) |
| Public path | `/fs/...` — `app.UseStaticFiles(...)` in `Program.cs` maps the Uploads folder |
| Subfolders | `CompanyLogo/`, `Profile/`, `Resume/`, … |
| URL helpers | `Common.GetCompanyLogoUrl(filename)`, similar in [`Common.cs`](../Codeji.CMS.Utility/Common.cs) |

Files are written by service code (e.g. `EmployeeService.UpdateProfilePicture`). No CDN; for high-volume serving, plan for blob storage + signed URLs.

---

## Rate limiting / throttling

**Not implemented.** No `AddRateLimiter()` in `Program.cs`. Add it before exposing rate-sensitive endpoints (login, registration, password-reset) to the public internet.

---

## Health checks

**Not implemented.** No `MapHealthChecks("/health")`. Add a basic `AddHealthChecks().AddMongoDb(...)` for liveness/readiness probes when deploying to Kubernetes / Azure App Service slots.
