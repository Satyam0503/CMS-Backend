# Conventions

Patterns observed across the codebase. Follow these in new code unless you have a strong reason not to.

---

## Project boundaries (one-way dependencies)

```
API ──► Services ──► Repository ──► DTO ──► Utility
```

- **DTO** depends only on Utility. Never inject anything; it's pure data.
- **Repository** depends on DTO + Utility. No service references — the repository never calls business logic.
- **Services** depend on Repository + DTO + Utility. The full business layer.
- **API** depends on Services + DTO + Utility. Controllers + middleware.
- **Migrations** depends only on Repository (so it has access to entities + the `Result` type).

If you find yourself needing a backwards reference (e.g. Repository wants to call a Service), the design is wrong — push the orchestration up to the Service layer.

---

## File & folder naming

| Kind | Convention | Example |
|---|---|---|
| Project | `Codeji.CMS.<Layer>` | `Codeji.CMS.Services` |
| Controller | `<Domain>Controller.cs`, inherits `BaseApiController` | `EmployeeService.cs` |
| Service interface | `I<Domain>Service.cs` (or `Services.cs`), under `Interface/` subfolder | `IEmployeeService.cs` |
| Service implementation | `<Domain>Service.cs` (or `Services.cs`), at folder root | `EmployeeService.cs` |
| Repository (custom) | `<Domain>Repository.cs` + `I<Domain>Interface.cs` | `AttendanceRepository.cs` |
| Entity | `<Noun>.cs` (singular), under `Entities/<Domain>/` | `EmpUser.cs`, `LeavePolicy.cs` |
| DTO request | `<Verb><Noun>Request[Dto].cs` or `<Noun>RequestModel.cs` | `AddNoticeRequest.cs`, `LeavePolicyRequest.cs` |
| DTO response | `<Noun>Response[Dto].cs` or `<Noun>ViewModel.cs` | `LeaveResponseDto.cs`, `ApplicantViewModel.cs` |
| Migration | `<Verb><Noun>.cs` under `Codeji.CMS.Migrations/Migrations/` | `SeedBaseModulesAndPermissions.cs` |
| Helper | `<Domain>Handler.cs` / `<Domain>Helper.cs` / `<Domain>Manager.cs` | `AuthenticationHandler.cs`, `ConfigManager.cs` |

Suffixes are inconsistent in places (some say `Service`, some `Services`; some `Dto`, some not). New code should pick one and match the surrounding folder.

---

## Controller pattern

```csharp
[Authorize]
[Route("api/[controller]")]
public class EmployeeController : BaseApiController
{
    private readonly IEmployeeService _employeeService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeController(IEmployeeService employeeService, IHttpContextAccessor httpContextAccessor)
    {
        _employeeService = employeeService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("InviteNewEmployee")]
    [ModulePermission(AppModule.Employees, Permission.Create)]
    public async Task<Result<InviteEmployeeDto>> InviteNewEmployee([FromBody] InviteEmployeeDto model)
    {
        var result = new Result<InviteEmployeeDto>();
        if (!ModelState.IsValid)
        {
            result.Success = false;
            result.Message = "Invalid input.";
            result.StatusCode = StatusCodes.Status400BadRequest;
            return result;
        }

        return await _employeeService.InviteNewEmployee(model, CurrentContext.UserId(_httpContextAccessor));
    }
}
```

Rules:

- **Inherit `BaseApiController`** (currently empty, but exists for cross-cutting hook points later).
- **Class-level `[Authorize]`** unless explicitly public — and call out `[AllowAnonymous]` on each public action.
- **Class-level `[Route("api/[controller]")]`** — the convention; some older controllers use `[Route("api")]` with per-action routes, those should be migrated when touched.
- **Action-level `[ModulePermission(...)]`** for fine-grained access — never assume "the whole controller is `Employees`."
- **Async**, returning `Task<Result<T>>` or `Task<Result>`.
- **Thin** — validate `ModelState` (or skip if your DTOs lack annotations and rely on service validation), then call the service. No business logic.
- **Use `CurrentContext`** for `UserId`/`CompanyId`/`RoleId` — never `User.Claims` directly.

---

## Service pattern

```csharp
public class EmployeeService : IEmployeeService
{
    private readonly IMongoDbRepository<EmpUser> _employeeRepository;
    private readonly IMongoDbRepository<Company> _companyRepository;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPriorityTaskQueue _priorityTaskQueue;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(/* … all of the above … */)
    {
        // assignments
    }

    public async Task<Result<InviteEmployeeDto>> InviteNewEmployee(InviteEmployeeDto model, string currentUserId)
    {
        var result = new Result<InviteEmployeeDto>();

        // 1. Sanitize HTML/text DTO fields if any have [Sanitize]
        Sanitizer.SanitizeProperties(model);

        // 2. Business validation (use Result.StatusCode for known failure modes)
        if (await _employeeRepository.Exist(e => e.Email == model.Email))
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.EmployeeWithSameEmailExists;
            result.Message = "Employee with this email already exists.";
            return result;
        }

        // 3. Map and persist
        var entity = _mapper.Map<EmpUser>(model);
        var addResult = await _employeeRepository.AddOne(entity);
        if (!addResult.Success) return new Result<InviteEmployeeDto> { Success = false, Message = addResult.Message };

        // 4. Side effects via priority queue (don't block the response)
        _priorityTaskQueue.QueueBackgroundWorkItem(async ct =>
        {
            await SendWelcomeEmail(entity);
        }, priority: 1);

        result.Success = true;
        result.MethodResult = model;
        return result;
    }
}
```

Rules:

- **Constructor DI only** — no service locator pattern, no static state.
- **Inject `IMongoDbRepository<TEntity>`** for each entity you touch — open generic, no per-entity registration.
- **Inject `ILogger<TService>`** when the service does anything async/background or anything where failures are silent.
- **Always return `Result<T>` or `Result`** — never raw entities or `Task<TEntity>`. Errors are encoded in `Success/StatusCode/Message`, not thrown.
- **Sanitize before save** if any DTO field has `[Sanitize]`.
- **Side effects go through `IPriorityTaskQueue`** — don't `await` mail/SignalR/PDF inside the request path.
- **Methods named verb-first**: `CreateXxx`, `GetXxx`, `UpdateXxx`, `DeleteXxx`.

---

## Result pattern

[`Result`](../Codeji.CMS.Repository/Domain/Result.cs) is the universal envelope. The frontend (every service file) does `if (data.success && data.statusCode === 200) { … }`.

| Property | When to set |
|---|---|
| `Success = true` | Operation completed without business-logic failure |
| `Success = false` | Validation failure, conflict, not-found, permission edge case |
| `StatusCode` | Standard HTTP code (200, 400, 401, 403, 404, 500) **or** a custom code from [`CustomStatusCode.cs`](../Codeji.CMS.Utility/Helpers/CustomStatusCode.cs) for app-specific situations (e.g. `EmployeeWithSameEmailExists = 4001`) |
| `Message` | Human-readable, optionally i18n key — surfaced in toast on the frontend |
| `MethodResult` | Single-record response |
| `MethodResults` | List response |
| `TotalRecords` | Pagination total (for list responses) |

**Don't throw** for known business failures. Throw for:
- `NullReferenceException` and friends (real bugs).
- Catastrophic infrastructure failures (DB down).

These are caught by [`ExceptionHandlingMiddleware`](../Codeji.CMS.API/App_Start/ExceptionHandlingMiddleware.cs) and returned as a generic 500.

---

## Validation

Two layers, used together:

1. **DTO data annotations** — `[Required]`, `[StringLength]`, `[EmailAddress]`, `[Range]`, `[RegularExpression]`. Checked by `ModelState.IsValid` in the controller.
2. **Service-side business rules** — anything that requires a DB query (uniqueness, "is this user authorized to act on this record", "is the leave balance sufficient") goes in the service.

**No FluentValidation** — keep it simple with annotations.

> **Caveat.** Many DTOs in [`Codeji.CMS.DTO/`](../Codeji.CMS.DTO/) lack annotations entirely. Adding them is one of the highest-value cleanup passes — most controllers do `if (!ModelState.IsValid) return result;` even when the DTO has no rules attached, so the check is a no-op.

---

## AutoMapper

Single profile in [`AutoMapperObjects.cs`](../Codeji.CMS.Services/Registration/AutoMapperObjects.cs):

```csharp
CreateMap<EmpUser, GetAllEmployeeResponseModel>().ReverseMap();

CreateMap<Applicant, ApplicantViewModel>()
    .ForMember(d => d.ApplyDate, opt => opt.MapFrom(s => s.CreatedDate))
    .ReverseMap();

// Partial-update DTO → entity, ignoring null source members
CreateMap<AttendanceUpdateDto, AttendanceModel>()
    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
```

Rules:

- **Bidirectional** with `.ReverseMap()` unless one direction is meaningless.
- **Custom property mapping** with `.ForMember(...)`.
- **Update DTOs** that allow partial updates — use `.ForAllMembers(opts => opts.Condition((s, d, sm) => sm != null))`.
- **Never map `EmpUser.Password` into a response DTO.** Always project to a DTO that doesn't have a `Password` field.
- **Never map a DTO directly to an entity for *update*** when you want partial semantics — use `Builders<T>.Update.Set(...)` against `_repo.UpdateMany(...)` instead. Whole-document replace is destructive.

---

## Async / await

- **Async all the way.** No `.Result`, no `.Wait()`. Two known offenders flagged in the issues list — fix in place when you touch them.
- **Async controller actions** return `Task<Result<T>>` — no `void` actions.
- **Don't fire-and-forget without awareness.** `_ = SomeMethodAsync();` discards exceptions. Use `IPriorityTaskQueue` instead, which logs failures.
- **Cancellation tokens.** Hosted services receive `CancellationToken stoppingToken` — pass through to long-running operations. Most service methods don't currently take CTs; that's a future cleanup.

---

## Logging

Pattern:

```csharp
_logger.LogInformation("Inviting new employee {Email} for company {CompanyId}", email, companyId);
_logger.LogError(ex, "Failed to invite employee {Email}", email);
```

- **Structured logging** with `{Placeholder}` — never string interpolation in the message.
- **`LogInformation`** for state changes worth observing in prod.
- **`LogError`** for caught exceptions — pass the exception as the first arg.
- **`LogWarning`** for recoverable failures (e.g. mail send failed but request succeeded).
- Avoid `LogDebug` and `LogTrace` for now — there's no log-level config in `appsettings.json` distinguishing dev from prod, and noise becomes noise everywhere.

---

## Multi-tenant queries

The repository takes care of `CompanyId`. **You should not** add `entity.CompanyId == companyId` clauses to your service-side filters — duplicating it is harmless but obscures the intent and rots if the repository changes. Trust the abstraction.

The exceptions:
- Migrations bypass the abstraction (no `IHttpContextAccessor`).
- Cross-tenant admin reports — declare them clearly and route them through a dedicated `*MasterService` that uses raw `_repo.GetCollection().Find(...)`.

---

## Sanitization

Any DTO field that holds user-authored HTML must have `[Sanitize]` and the receiving service must call `Sanitizer.SanitizeProperties(dto)` **before persisting**.

```csharp
public class AddNoticeRequest
{
    public string Title { get; set; }

    [Sanitize]
    public string Body { get; set; }
}
```

Don't sanitize on read — by then the bytes are already in the DB and you'd just hide a problem. Sanitize at the point of write.

---

## i18n / multilingual fields

Some entities (Department, JobTitles) hold a `List<MultilingualModel>` instead of a single string:

```csharp
public List<MultilingualModel> Titles { get; set; }   // [{ Language: "en", Label: "Engineering" }]
```

When seeding or accepting input, support every language listed in `Company.ApplicationLanguage` (or fall back to `DefaultLanguage`). [`DefaultCompanySeeds.cs`](../Codeji.CMS.Services/Companies/DefaultCompanySeeds.cs) is the reference for translating at write time.

Read paths typically pick the matching `Language` for the request locale (from `Accept-Language` or the user's profile).

---

## DTO conventions

- **One DTO per use case** — don't reuse `XxxRequestDto` for create *and* update; the validation rules are different.
- **`XxxRequest` vs `XxxRequestDto` vs `XxxRequestModel`** — pick one suffix per folder, match neighbors.
- **Suffix `Filter` for filter / search models** — e.g. `LeaveBalanceFilter`, `ApplicantResultFilters`.
- **Fields named in camelCase or PascalCase?** PascalCase here (C# convention); JSON serializer maps to camelCase on the wire by default.
- **Don't expose internal IDs you don't need.** A response DTO doesn't need `_id` if the public ID is `EmployeeId` or similar.

---

## Don't do these

- **Don't return entities directly from a controller.** Always wrap in `Result<TDto>`.
- **Don't catch `Exception` and continue.** Either log + rethrow, or convert to a `Result` failure with a meaningful `StatusCode`. Empty catches mask bugs.
- **Don't log secrets.** Passwords, tokens, JWTs, BCrypt hashes — never in log messages.
- **Don't introduce new MongoDB collections without a corresponding entity class.** The collection name comes from `typeof(T).Name`; raw collection access scattered around the codebase is a maintenance trap.
- **Don't reference `DateTime.Now`.** Use `DateTime.UtcNow` for everything stored or compared. The frontend handles timezone display.
- **Don't access `HttpContext` from a service.** Inject `IHttpContextAccessor` and use `CurrentContext`.
- **Don't put validation logic in the controller.** Annotate the DTO or push to the service.
- **Don't add a third Result variant.** Use `Result` (no data) or `Result<T>` (with data) — `GetOneResult<T>`/`GetManyResult<T>` are legacy.
