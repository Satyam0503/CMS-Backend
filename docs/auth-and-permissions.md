# Auth & Permissions

Three layers of access control, all server-side, all enforced before the controller body runs:

1. **JWT bearer auth** — does the request have a valid token?
2. **Policy-based authorization** — is the user the right kind (e.g. `AdminOnly`)?
3. **Module + permission attribute** — does the user's role grant the specific action on the specific feature?

---

## JWT setup

Configured in [Program.cs](../Codeji.CMS.API/Program.cs) (search for `AddJwtBearer`):

```csharp
builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer            = true,
          ValidateAudience          = true,
          ValidateLifetime          = true,
          ValidateIssuerSigningKey  = true,
          ValidIssuer               = ConfigManager.AppSettings.APIUrl,   // backend URL
          ValidAudience             = ConfigManager.AppSettings.AppUrl,   // frontend URL
          IssuerSigningKey          = new SymmetricSecurityKey(
                                        Encoding.UTF8.GetBytes(ConfigManager.Jwt.SecretKey)),
      };
      options.Events = new JwtBearerEvents 
      {
          OnMessageReceived = ctx =>
          {
              var accessToken = ctx.Request.Query["access_token"];
              var path = ctx.HttpContext.Request.Path;
              if (!string.IsNullOrEmpty(accessToken)
                  && (path.StartsWithSegments("/notificationhub")
                      || path.StartsWithSegments("/chathub")))
              {
                  ctx.Token = accessToken;
              }
              return Task.CompletedTask;
          }
      };
  });
```

Notable choices:

- **HMAC-SHA256** signing using `Jwt.SecretKey` from `appsettings.json`. Must be ≥256 bits (32 chars).
- **Issuer / audience tied to URLs**. If you change `APIUrl` or `AppUrl` in config, all currently-issued tokens become invalid (every user has to log in again). Use *logical* identifiers in production if zero-downtime config changes matter.
- **Query-string token for SignalR**. WebSocket clients can't set the `Authorization` header — they pass `?access_token=...` instead. Only allowed for `/notificationhub` and `/chathub` paths.

### Settings

[`ConfigManager`](../Codeji.CMS.Utility/Helpers/ConfigManager.cs) `Jwt` block (loaded from `appsettings.json`):

| Key | Purpose |
|---|---|
| `SecretKey` | HMAC signing key |
| `Expiry` | Access-token lifetime in **minutes** (default 15) |
| `RefreshTokenExpiry` | Refresh-token lifetime in days (default 7) |

---

## Token generation

[`AuthenticationHandler.GenerateJwtToken(userId, companyId, roleId, userRole)`](../Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs):

```csharp
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, userId),
    new("user_id",    userId),
    new("company_id", companyId),
    new("role_id",    roleId),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
};
foreach (var role in userRole) claims.Add(new Claim(ClaimTypes.Role, role));

var creds = new SigningCredentials(
    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigManager.Jwt.SecretKey)),
    SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer:   ConfigManager.AppSettings.APIUrl,
    audience: ConfigManager.AppSettings.AppUrl,
    claims:   claims,
    expires:  DateTime.UtcNow.AddMinutes(ConfigManager.Jwt.Expiry),
    signingCredentials: creds);

return new JwtSecurityTokenHandler().WriteToken(token);
```

Claims travel with the token:

| Claim | Purpose | Read with |
|---|---|---|
| `sub` | Standard subject | n/a (use `user_id`) |
| `user_id` | Current user ID | `CurrentContext.UserId(httpContextAccessor)` |
| `company_id` | Tenant ID | `CurrentContext.CompanyId(...)` — used by the repo |
| `role_id` | Role ID for permission lookup | `CurrentContext.UserRoleId(...)` |
| `ClaimTypes.Role` (1+) | Role name(s) for `[Authorize(Roles = ...)]` | ASP.NET Core built-ins |
| `jti` | Unique token ID | n/a |

[`CurrentContext.cs`](../Codeji.CMS.Utility/middlewares/CurrentContext.cs) is the only place these claims should be read from. Don't poke `User.Claims` directly in services — use the helper.

---

## Password hashing

[`AuthenticationHandler.HashedPassword`](../Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs):

```csharp
public static string HashedPassword(string pass) => BCrypt.Net.BCrypt.HashPassword(pass);
public static bool VerifyPassword(string toMatch, string forMatch)
    => BCrypt.Net.BCrypt.Verify(toMatch, forMatch);
```

- **BCrypt.Net-Next 4.0.3** — salt embedded in hash, timing-safe verify.
- Default cost factor (10). Bumping it requires nothing here — BCrypt encodes the cost in the hash and `Verify` reads it back.
- `Password` is stored on [`EmpUser`](../Codeji.CMS.Repository/Entities/Employees/EmpUser.cs) as the BCrypt hash string. Never logged, never returned in any DTO.

---

## Refresh-token flow

[`RefreshToken`](../Codeji.CMS.Repository/Entities/RefreshToken.cs) entity:

```csharp
public class RefreshToken
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string Token { get; set; }
    public required string UserId { get; set; }
    public required DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
}
```

End-to-end:

1. **Login** (`POST api/account/login`):
   - `AccountServices.VerifyAndGenerateToken()` validates credentials.
   - Generates a short-lived JWT (15 min default) **and** a long-lived refresh token (7 days default).
   - Stores the refresh token in the `RefreshToken` collection.
   - Returns both as `TokenResponseDto`.

2. **Normal API call**:
   - Frontend sends JWT as `Authorization: Bearer …`.
   - `JwtBearer` middleware validates the token. Expired token → 401.

3. **On 401**:
   - Frontend (axios interceptor) calls `POST api/account/refresh-token` with the refresh token.
   - `AccountServices.RefreshToken()` looks the token up in MongoDB, checks `!IsRevoked && ExpireAt > UtcNow`, and reads the linked user.
   - Issues a new JWT. The refresh token may be rotated or reused — check the current implementation.
   - Returns the new JWT.
   - Frontend retries the original request with the new bearer.

4. **Logout / revocation**:
   - Set `IsRevoked = true` and `RevokedAt = UtcNow` on the row.
   - Future refresh attempts with that token are rejected.

> **Caveat.** The current refresh-token implementation does not invalidate the old refresh token after issuing a new one — once stolen, a refresh token works for its full lifetime. Token rotation is a worthwhile hardening if you ever store refresh tokens client-side in less-secure places.

---

## ModulePermission attribute

The fine-grained gate. Defined in [`ModulePermissionAttribute.cs`](../Codeji.CMS.API/App_Start/ModulePermissionAttribute.cs):

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class ModulePermissionAttribute : Attribute
{
    public string Module { get; }
    public string[] Permissions { get; }

    public ModulePermissionAttribute(string module, params string[] permissions)
    {
        Module = module;
        Permissions = permissions;
    }
}
```

Usage on a controller action:

```csharp
[ModulePermission(AppModule.Employees, Permission.Create)]
public async Task<Result<InviteEmployeeDto>> InviteNewEmployee([FromBody] InviteEmployeeDto model) { … }

[ModulePermission(AppModule.Employees, Permission.Edit, Permission.Create)]   // OR-ed
public async Task<Result> UpsertEmployee(...) { … }
```

The attribute itself is **metadata only** — no logic. The actual check happens in:

[`AuthenticateUserRequest.cs`](../Codeji.CMS.API/App_Start/AuthenticateUserRequest.cs):

```csharp
var endpoint = context.GetEndpoint();
if (endpoint != null && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(companyId))
{
    var attrs = endpoint.Metadata.GetOrderedMetadata<ModulePermissionAttribute>();
    if (attrs == null || !attrs.Any()) return context;

    var roleService = (IRoleService)context.RequestServices.GetService(typeof(IRoleService));
    var attr = attrs.First();

    var ok = await roleService.VerifyUserAccess(attr.Module, attr.Permissions, userId, companyId);
    if (!ok)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Access Denied");
        return null;
    }
}
```

`IRoleService.VerifyUserAccess(module, permissions[], userId, companyId)`:
1. Resolves the user's role.
2. Joins `RolePermission → ModulePermission → Module + Permission`.
3. Returns `true` if **any** required permission has `HasAccess == true`.

---

## The permission data model

```
Module (global)              Permission (global)
  ModuleId, ModuleConstant     PermissionId, PermissionConstant
        │                              │
        └──────► ModulePermission ◄────┘
                   ModulePermissionId, ModuleId, PermissionId
                          │
                          ▼
                    RolePermission (per company)
                       RoleId, ModulePermissionId, HasAccess, IsAccessible, CompanyId
                          │
                          ▼
                       Roles (per company)
                         RolesId, CompanyId, RoleType, Titles
                          │
                          ▼
                       EmpUser
                         RoleId
```

Seeded by:
- **Global side** (Permission, Module, ModulePermission): [SeedBaseModulesAndPermissions.cs](../Codeji.CMS.Migrations/Migrations/SeedBaseModulesAndPermissions.cs).
- **Per-company side** (Roles + RolePermission grid): [`RoleServices.AddDefaultRole(companyId)`](../Codeji.CMS.Services/Employees/RoleServices.cs), called by `CompanyService.Register()`.
- **Adding a module later**: [AddPolicyModuleAndPermissions.cs](../Codeji.CMS.Migrations/Migrations/AddPolicyModuleAndPermissions.cs) is the template — adds the `Module`, the `ModulePermission` rows, then back-fills `RolePermission` for every existing company's roles.

`HasAccess` is the grant; `IsAccessible` is whether the company has *enabled* the module at all (a layer above per-role grants — admin can flip a module off for everyone). Both must be true for access.

---

## AdminOnly policy

[`CustomAuthorizationPolicy.cs`](../Codeji.CMS.API/App_Start/CustomAuthorizationPolicy.cs) registers:

```csharp
options.AddPolicy("AdminOnly", policy =>
    policy.Requirements.Add(new RoleRequirement(EnumsHelper.Roles.Administrator)));
```

The `RoleHandler` reads the `role_id` claim, looks up the role's `RoleType`, and authorizes if it equals `Administrator`. Use:

```csharp
[Authorize(Policy = "AdminOnly")]
public class CompanyMasterController : BaseApiController { … }
```

---

## Antiforgery (CSRF)

Setup in [Program.cs](../Codeji.CMS.API/Program.cs):

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "XSRF-TOKEN";
    options.Cookie.Name = "X_CSRFTken";   // typo, but in production. Don't rename without coordinated frontend change
});
```

Token-issuance endpoint: `GET api/account/antiforgerytoken/{appKey}` — returns the request token (in body) and sets the cookie. The frontend echoes the body back in the `XSRF-TOKEN` header on state-changing requests; [`AntyForgeryMiddleware.cs`](../Codeji.CMS.API/App_Start/AntyForgeryMiddleware.cs) validates.

> **Caveat.** The `appKey` is hardcoded to `"uiploutssh-817181871"` ([AccountController.cs:76](../Codeji.CMS.API/Controllers/AccountController.cs#L76)). Move to config when you have a chance.

---

## CORS

[Program.cs](../Codeji.CMS.API/Program.cs):

```csharp
builder.Services.AddCors(option => option.AddPolicy("codeji", b =>
{
    b.AllowCredentials()
     .SetIsOriginAllowed(origin =>
     {
         var host = new Uri(origin).Host;
         return host == "localhost" || host.EndsWith(".codeji.in");
     })
     .AllowAnyHeader()
     .AllowAnyMethod();
}));
```

Whitelisted: `localhost` (any port) + any `*.codeji.in` subdomain. New deployment domains need to be added here.

`AllowAnyMethod()` + `AllowAnyHeader()` are intentionally broad — tighten if you start handling third-party browser callers.

---

## SignalR auth

`/notificationhub` and `/chathub` are JWT-protected. Browsers can't attach an `Authorization` header to a WebSocket upgrade — the frontend appends `?access_token=…` to the URL and the `OnMessageReceived` event in `AddJwtBearer` extracts it.

User identity for SignalR is provided by [`GetUserIdProvider.cs`](../Codeji.CMS.API/App_Start/GetUserIdProvider.cs), which reads the `sub` claim. So `IHubContext<T>.Clients.User(userId)` targets a specific user across all their connections.

---

## Reading current context inside a service

Always via [`CurrentContext`](../Codeji.CMS.Utility/middlewares/CurrentContext.cs):

```csharp
public class MyService(IHttpContextAccessor httpContextAccessor)
{
    public async Task DoThing()
    {
        var userId    = CurrentContext.UserId(httpContextAccessor);
        var companyId = CurrentContext.CompanyId(httpContextAccessor);
        var roleId    = CurrentContext.UserRoleId(httpContextAccessor);
        // ...
    }
}
```

The repository already injects `CompanyId` automatically; you only need it in the service when the business logic itself depends on it (e.g. cross-tenant admin reports, audit logs that capture who acted).

---

## Common mistakes

- **Forgetting `[ModulePermission]`** on a new endpoint → it inherits whatever class-level attributes exist (often just `[Authorize]`), which means *any* logged-in user can call it. Always add the attribute even when permission is "obvious."
- **Mismatched `AppModule` constant** between code and DB → `VerifyUserAccess` returns false, you see 403s for everyone. Check [`ConstraintHelper.cs`](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs) and the `Module` collection match.
- **Using `User.Claims` directly** in a service → works until someone changes claim names. Use `CurrentContext`.
- **Returning the password hash in any DTO** → don't. Mapster happily maps `EmpUser.Password` if you map the whole entity. Always project to a DTO that omits it.
- **Long token lifetimes** → `Jwt.Expiry > 60` minutes is suspicious. Refresh tokens cover the long-lived case.
