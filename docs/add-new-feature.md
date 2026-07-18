# Add a New Feature: The Playbook

The concrete recipe for shipping a new business module on the backend. Examples assume a hypothetical "Assets" module (laptops / phones issued to employees). Replace `Assets` / `Asset` with your real names.

> Before starting, skim [`conventions.md`](./conventions.md), [`auth-and-permissions.md`](./auth-and-permissions.md), and [`data-layer.md`](./data-layer.md). Most decisions are already made for you — just follow the patterns.

---

## 1. Decide the boundary

Ask:

- Is this a **new business domain** (new entity + new endpoints)? → Full module.
- Is this **another endpoint on an existing controller**? → Skip to step 5 below.
- Is it **just a new field on an existing entity**? → Add the property + Mapster mapping + controller DTO change. No migration needed (Mongo ignores missing fields). No new module.

Below assumes "new business domain."

---

## 2. Define the entity

Create [`Codeji.CMS.Repository/Entities/Assets/Asset.cs`](../Codeji.CMS.Repository/Entities/):

```csharp
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Assets;

public class Asset : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string AssetId { get; set; }

    public required string SerialNumber { get; set; }
    public required string Model { get; set; }
    public AssetType Type { get; set; }
    public string? AssignedToUserId { get; set; }
    public DateTime? AssignedDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum AssetType
{
    Laptop = 1,
    Phone = 2,
    Monitor = 3,
}
```

Rules:
- **Inherit `BaseClass`** for multi-tenancy + audit + soft-delete.
- **`[BsonId(IdGenerator = typeof(UniqueIdGenerator))]`** on the public ID property.
- **Don't add `CompanyId`, `CreatedBy`, etc.** — they come from `BaseClass`.

If `AssetType` is referenced from more than one place, hoist it to [`EnumsHelper.cs`](../Codeji.CMS.Utility/Enums/EnumsHelper.cs).

---

## 3. Add module + permission constants

[`ConstraintHelper.cs`](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs):

```csharp
public static class AppModule
{
    // … existing …
    public const string Assets = "Assets";
}
```

The string value will be the `ModuleConstant` in the `Module` collection. **Must match exactly** in the migration and on the frontend (`ModulesName.Assets`).

---

## 4. Define DTOs

Create [`Codeji.CMS.DTO/Assets/`](../Codeji.CMS.DTO/) and add:

```csharp
// AddAssetRequestDto.cs
public class AddAssetRequestDto
{
    [Required] public string SerialNumber { get; set; }
    [Required] public string Model { get; set; }
    [Required] public AssetType Type { get; set; }
}

// UpdateAssetRequestDto.cs (allow partial updates)
public class UpdateAssetRequestDto
{
    [Required] public string AssetId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public AssetType? Type { get; set; }
    public string? AssignedToUserId { get; set; }
    public bool? IsActive { get; set; }
}

// AssetResponseDto.cs
public class AssetResponseDto
{
    public string AssetId { get; set; }
    public string SerialNumber { get; set; }
    public string Model { get; set; }
    public AssetType Type { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }   // joined from EmpUser
    public DateTime? AssignedDate { get; set; }
    public bool IsActive { get; set; }
}

// AssetFilterDto.cs
public class AssetFilterDto
{
    public string? SearchText { get; set; }
    public AssetType? Type { get; set; }
    public bool? IsAssigned { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

Rules:
- **Data annotations** for validation that doesn't need the DB.
- **Separate Create vs Update vs Response.** Don't reuse one DTO across all three.
- **Filter DTOs end in `Filter`** or `FilterDto`.

---

## 5. Add Mapster config

Edit [`MapsterConfig.cs`](../Codeji.CMS.Services/Registration/MapsterConfig.cs), add:

```csharp
config.NewConfig<AddAssetRequestDto, Asset>().TwoWays();

config.NewConfig<UpdateAssetRequestDto, Asset>()
    .IgnoreNullValues(true);

config.NewConfig<Asset, AssetResponseDto>().TwoWays();
```

The `IgnoreNullValues` setting on the update mapping is what makes partial updates work — it skips null fields rather than overwriting the entity.

---

## 6. Write the service

Create the interface [`Codeji.CMS.Services/Assets/Interface/IAssetService.cs`](../Codeji.CMS.Services/):

```csharp
public interface IAssetService
{
    Task<Result<AssetResponseDto>> GetAllAssets(AssetFilterDto filter);
    Task<Result<AssetResponseDto>> GetAssetById(string assetId);
    Task<Result> CreateAsset(AddAssetRequestDto dto);
    Task<Result> UpdateAsset(UpdateAssetRequestDto dto);
    Task<Result> DeleteAsset(string assetId);
}
```

Create the implementation [`Codeji.CMS.Services/Assets/AssetService.cs`](../Codeji.CMS.Services/):

```csharp
public class AssetService : IAssetService
{
    private readonly IMongoDbRepository<Asset> _assetRepo;
    private readonly IMongoDbRepository<EmpUser> _userRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        IMongoDbRepository<Asset> assetRepo,
        IMongoDbRepository<EmpUser> userRepo,
        IMapper mapper,
        ILogger<AssetService> logger)
    {
        _assetRepo = assetRepo;
        _userRepo  = userRepo;
        _mapper    = mapper;
        _logger    = logger;
    }

    public async Task<Result<AssetResponseDto>> GetAllAssets(AssetFilterDto filter)
    {
        var result = new Result<AssetResponseDto>();

        var query = _assetRepo.Get(a =>
            (filter.Type == null || a.Type == filter.Type) &&
            (string.IsNullOrEmpty(filter.SearchText)
                || a.SerialNumber.Contains(filter.SearchText)
                || a.Model.Contains(filter.SearchText)));

        result.TotalRecords = query.Count();
        var page = query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        result.MethodResults = _mapper.Map<List<AssetResponseDto>>(page);
        return result;
    }

    public async Task<Result> CreateAsset(AddAssetRequestDto dto)
    {
        if (await _assetRepo.Exist(a => a.SerialNumber == dto.SerialNumber))
            return new Result { Success = false, Message = "Serial number already exists.", StatusCode = 409 };

        var asset = _mapper.Map<Asset>(dto);
        return await _assetRepo.AddOne(asset);
    }

    public async Task<Result> UpdateAsset(UpdateAssetRequestDto dto)
    {
        var existing = await _assetRepo.FirstOrDefault(a => a.AssetId == dto.AssetId);
        if (existing is null)
            return new Result { Success = false, Message = "Not found.", StatusCode = 404 };

        _mapper.Map(dto, existing);   // partial map, ignores nulls
        return await _assetRepo.Update(Builders<Asset>.Filter.Eq(a => a.AssetId, dto.AssetId), existing);
    }

    public async Task<Result> DeleteAsset(string assetId) =>
        await _assetRepo.UpdateMany(
            a => a.AssetId == assetId,
            Builders<Asset>.Update.Set(a => a.IsDeleted, true));   // soft delete
}
```

Rules:
- **Inject `IMongoDbRepository<Asset>`** — DI handles it via the open generic.
- **`Result` / `Result<T>`** as return type — never raw entities or exceptions for business failures.
- **Soft delete** (`UpdateMany` setting `IsDeleted = true`) — not `Delete()`.
- **`ILogger<AssetService>`** for any non-trivial path.

---

## 7. Register the service in DI

Open [`ServicesRegistration.cs`](../Codeji.CMS.Services/Registration/ServicesRegistration.cs) and add:

```csharp
services.AddScoped<IAssetService, AssetService>();
```

`Scoped` is the right lifetime for almost all services (one instance per request). Singleton is dangerous (state across tenants); transient creates churn.

---

## 8. Write the controller

Create [`Codeji.CMS.API/Controllers/AssetsController.cs`](../Codeji.CMS.API/Controllers/):

```csharp
[Authorize]
[Route("api/[controller]")]
public class AssetsController : BaseApiController
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpPost("GetAllAssets")]
    [ModulePermission(AppModule.Assets, Permission.View)]
    public async Task<Result<AssetResponseDto>> GetAllAssets([FromBody] AssetFilterDto filter)
        => await _assetService.GetAllAssets(filter);

    [HttpPost("CreateAsset")]
    [ModulePermission(AppModule.Assets, Permission.Create)]
    public async Task<Result> CreateAsset([FromBody] AddAssetRequestDto dto)
    {
        if (!ModelState.IsValid)
            return new Result { Success = false, Message = "Invalid input.", StatusCode = 400 };
        return await _assetService.CreateAsset(dto);
    }

    [HttpPut("UpdateAsset")]
    [ModulePermission(AppModule.Assets, Permission.Edit)]
    public async Task<Result> UpdateAsset([FromBody] UpdateAssetRequestDto dto) =>
        await _assetService.UpdateAsset(dto);

    [HttpDelete("DeleteAsset/{assetId}")]
    [ModulePermission(AppModule.Assets, Permission.Delete)]
    public async Task<Result> DeleteAsset(string assetId) =>
        await _assetService.DeleteAsset(assetId);
}
```

Rules:
- **`[Authorize]` at class level** — every endpoint needs auth unless explicitly `[AllowAnonymous]`.
- **`[ModulePermission(...)]` on every action** — not at class level (different verbs need different permissions).
- **Route convention**: `api/[controller]` + `[HttpPost("ActionName")]` — matches the rest of the codebase.
- **Thin actions** — delegate to service immediately.

---

## 9. Write the migration

Create [`Codeji.CMS.Migrations/Migrations/AddAssetsModuleAndPermissions.cs`](../Codeji.CMS.Migrations/Migrations/) — copy the structure of [`AddPolicyModuleAndPermissions.cs`](../Codeji.CMS.Migrations/Migrations/AddPolicyModuleAndPermissions.cs):

```csharp
public class AddAssetsModuleAndPermissions : IMigration
{
    public string Id => $"2026-05-15-{typeof(AddAssetsModuleAndPermissions).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var modules           = db.GetCollection<Module>("Module");
        var permissions       = db.GetCollection<Permission>("Permission");
        var modulePermissions = db.GetCollection<ModulePermission>("ModulePermission");
        var roles             = db.GetCollection<Roles>("Roles");
        var rolePermission    = db.GetCollection<RolePermission>("RolePermission");

        // Skip row-level if already added (idempotent)
        if (await modules.Find(m => m.ModuleConstant == AppModule.Assets).AnyAsync())
            return;

        long totalModules = await modules.CountDocumentsAsync(FilterDefinition<Module>.Empty);
        long totalModulePermission = await modulePermissions.CountDocumentsAsync(FilterDefinition<ModulePermission>.Empty);
        var allPermissions = await permissions.Find(FilterDefinition<Permission>.Empty).ToListAsync();
        if (allPermissions.Count == 0) return;   // base seed hasn't run yet — bail

        // Insert Module
        var module = new Module
        {
            ModuleId = (int)totalModules + 1,
            ModuleName = AppModule.Assets,
            ModuleConstant = AppModule.Assets,
            SortOrder = (int)totalModules + 1,
        };
        await modules.InsertOneAsync(module);

        // Insert ModulePermission for each permission
        var modulePerms = allPermissions.Select(p => new ModulePermission
        {
            ModulePermissionId = (int)(++totalModulePermission),
            ModuleId = module.ModuleId,
            PermissionId = p.PermissionId,
            HasModuleAccess = true,
        }).ToList();
        await modulePermissions.InsertManyAsync(modulePerms);

        // Back-fill RolePermission for every existing role across every company
        var allRoles = await roles.Find(FilterDefinition<Roles>.Empty).ToListAsync();
        var rolePerms = new List<RolePermission>();
        foreach (var role in allRoles)
        {
            foreach (var mp in modulePerms)
            {
                bool isAdminOrHr = role.RoleType == (int)EnumsHelper.Roles.Administrator
                                || role.RoleType == (int)EnumsHelper.Roles.HR;
                bool isViewOnly  = mp.PermissionId == 1;   // View permission ID

                if (isAdminOrHr || isViewOnly)
                {
                    rolePerms.Add(new RolePermission
                    {
                        RoleId = role.RolesId,
                        ModulePermissionId = mp.ModulePermissionId,
                        HasAccess = true,
                        IsAccessible = true,
                        CompanyId = role.CompanyId,
                        CreatedDate = DateTime.UtcNow,
                    });
                }
            }
        }
        if (rolePerms.Count > 0)
            await rolePermission.InsertManyAsync(rolePerms);

        // Record the migration
        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}
```

Run it:

```bash
cd Codeji.CMS.Migrations
dotnet run
```

The runner will pick up the new class via reflection and apply it after all earlier-dated migrations.

---

## 10. Coordinate the frontend change

The backend is half. The frontend needs:

- A matching `ModulesName.Assets = 'Assets'` enum entry (must exactly match `AppModule.Assets`).
- `ServiceUrl.GET_ALL_ASSETS = 'Assets/GetAllAssets'` etc. paths matching the controller routes.
- A new module under `src/app/modules/assets/`.
- A route, sidebar entry, and translations.

See `CMS-React/docs/add-new-feature.md` for the full frontend recipe.

---

## 11. Verify

```bash
dotnet build                       # whole solution must build
cd Codeji.CMS.API && dotnet run    # API runs without crashing
# Hit the new endpoints via Swagger or Postman; verify:
#   - 401 if no token
#   - 403 if user's role lacks the permission
#   - 200 with expected payload otherwise
#   - 409 if you try to insert a duplicate serial number
#   - 404 if you update a missing asset
#   - Soft delete leaves the row in Mongo with IsDeleted=true
```

---

## 12. Update the docs

Open [`modules.md`](./modules.md) and add an "Assets" section using the existing template. Update the index table at the top of that file too. If the new module changes any cross-cutting pattern (rare), update the relevant doc.

This step is what keeps the docs trustworthy. Skipping it is how docs decay into lies.

---

## Pre-PR checklist

- [ ] Entity inherits `BaseClass`
- [ ] `[BsonId(IdGenerator = typeof(UniqueIdGenerator))]` on the public ID property
- [ ] DTOs separated for Create / Update / Response / Filter
- [ ] Update DTO uses nullable fields + `IgnoreNullValues(true)` Mapster config
- [ ] Service registered in `ServicesRegistration.cs`
- [ ] `AppModule.<Name>` constant added in `ConstraintHelper.cs`
- [ ] Controller has `[Authorize]` at class level, `[ModulePermission(...)]` on every action
- [ ] Migration is idempotent (checks `Migration` collection AND row-level natural key)
- [ ] No `DateTime.Now` (only `DateTime.UtcNow`)
- [ ] No raw entity exposed in any DTO; passwords / hashes never appear in any DTO
- [ ] Soft delete (`UpdateMany` setting `IsDeleted = true`), not `Delete()`
- [ ] Side-effects (mail, SignalR) go through `IPriorityTaskQueue`
- [ ] HTML-bearing fields decorated with `[Sanitize]`; service calls `Sanitizer.SanitizeProperties()` before save
- [ ] Logging via `ILogger<TService>` with structured placeholders
- [ ] [`modules.md`](./modules.md) updated

---

## Where to look when something doesn't work

| Symptom | Probable cause | File to check |
|---|---|---|
| 403 on every call to the new endpoint | `AppModule.Assets` string doesn't match the `Module.ModuleConstant` row | [`ConstraintHelper.cs`](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs) and the migration |
| 401 even after login | Token has expired and the refresh path doesn't include the new endpoint | [`axios.ts`](../../CMS-React/src/server/axios.ts) on the frontend |
| `MongoCollectionNotFoundException` or empty queries | The new entity inherits `BaseClass` but you're querying outside the repo and forgot the `CompanyId` filter | The service or controller code; switch to `_repo.GetAll(...)` |
| `Save()` succeeds but row never appears | You looked in the wrong collection. `MongoRepository<T>` writes to `typeof(T).Name` | The collection name in your DB explorer |
| Migration runs every startup | Forgot the `migrations.InsertOneAsync(new Migration { ... })` call at the end | The migration class |
| Mapster throws "missing map" at runtime | Forgot to register the map in `MapsterConfig.cs` | [`MapsterConfig.cs`](../Codeji.CMS.Services/Registration/MapsterConfig.cs) |
| Permission check passes when it shouldn't | `RolePermission.HasAccess` is true for that role+module — check the migration's back-fill logic | The migration that added the module |
| Email not sent but no error | The work item silently failed in the priority queue | Check `ILogger<EmailService>` output and the queue's running state |
