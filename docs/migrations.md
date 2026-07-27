# Migrations

A small custom migration runner — no Entity Framework, no Mongo migrations library. Each migration is a class implementing `IMigration`; the runner reflects on the assembly, sorts by `Id`, and executes in order. Each run records itself in the `Migration` collection so it never re-runs.

---

## Pieces

| File | Role |
|---|---|
| [`Interface/IMigration.cs`](../Codeji.CMS.Migrations/Interface/IMigration.cs) | The contract: `string Id`, `Task ExecuteAsync(IMongoDatabase db)` |
| [`MigrationLoader.cs`](../Codeji.CMS.Migrations/MigrationLoader.cs) | Reflects the assembly, collects `IMigration` types, instantiates, sorts by `Id` |
| [`MigrationRunner.cs`](../Codeji.CMS.Migrations/MigrationRunner.cs) | Iterates the sorted list and `await`s each `ExecuteAsync` |
| [`Program.cs`](../Codeji.CMS.Migrations/Program.cs) | CLI entry point — connects to MongoDB and calls the runner |
| [`Migration` entity](../Codeji.CMS.Repository/Entities/Migration.cs) | `{ Id, ExecutedAt }` row recording each successful run |

The runner has no DI, no logging framework, no concurrency control. Console output only.

---

## Ordering

`MigrationLoader.LoadMigrations()`:
 
```csharp
return Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(IMigration).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
    .Select(t => (IMigration)Activator.CreateInstance(t)!)
    .OrderBy(m => m.Id)
    .ToList();
```

Sort is **string-ordered on `Id`**. Convention: prefix with an ISO date (`YYYY-MM-DD-`) so chronological order = lexical order:

```csharp
public string Id => $"2026-05-01-{typeof(MyMigration).Name}";
```

This means **the date in the ID is what controls run order, not the file's creation date**. If you need a migration to run *between* two existing ones (a hotfix), pick a date that sorts in the right place — and avoid colliding with future planned migrations.

### Seed-first invariant

Seed migrations (the ones that populate global lookup data — `Permission`, `Module`, `ModulePermission`, `MailTemplate`) **must always sort before** module-add migrations. Module-add migrations look up rows from the seed tables when they run, and a fresh DB without the seeds applied first will fail.

This is enforced purely by the date prefix:

| Type | Date convention |
|---|---|
| Seed migrations | The earliest possible dates — currently `2024-12-01` and `2024-12-02` |
| Module-add migrations | A date >= today, in chronological order |

When introducing a new seed migration, give it a date older than all existing module-add migrations (or earlier than the seeds you want to depend on it). When introducing a new module-add migration, use today's date or later.

---

## Idempotency

The runner has no built-in skip mechanism. Each migration must:

1. Look itself up in the `Migration` collection by `Id`.
2. If found, return.
3. Do its work.
4. Insert its own `Migration` row.

The canonical shape:

```csharp
public async Task ExecuteAsync(IMongoDatabase db)
{
    var migrations = db.GetCollection<Migration>("Migration");
    if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

    // … do work …

    await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
}
```

A migration that adds rows to a lookup table should *also* be idempotent against the rows it inserts (skip rows already present by some natural key) — this lets the migration run safely against a partially-seeded database. The base-modules seed migration does this.

---

## How to run

```bash
cd Codeji.CMS.Migrations
dotnet run
```

`Program.cs`:

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connection = configuration.GetConnectionString("mongodb")
    ?? throw new InvalidOperationException("ConnectionStrings:mongodb is not configured.");
var mongoUrl = MongoUrl.Create(connection);
var db = new MongoClient(connection).GetDatabase(mongoUrl.DatabaseName);

var migrations = MigrationLoader.LoadMigrations();
var runner = new MigrationRunner(migrations, db);
await runner.RunAsync();
```

Reads MongoDB connection from `Codeji.CMS.Migrations/appsettings.json` (separate from the API's appsettings — make sure both point at the right DB for the environment you're running against).

Console output:

```
Running migration 2024-12-01-SeedBaseModulesAndPermissions...
Running migration 2024-12-02-SeedMailTemplates...
Running migration 2025-02-04-AddPayrollSettingseAndItsModulePermissions...
Running migration 2026-01-01-AddPolicyModuleAndItsModulePermissions...
All migrations completed.
```

Already-applied migrations early-return silently — no console line. Failures throw and the runner stops at that migration; everything before it has already been recorded.

---

## Existing migrations

| Id | File | What it does |
|---|---|---|
| `2024-12-01-SeedBaseModulesAndPermissions` | [SeedBaseModulesAndPermissions.cs](../Codeji.CMS.Migrations/Migrations/SeedBaseModulesAndPermissions.cs) | Seeds 4 `Permission` rows (View, Create, Edit, Delete), 9 `Module` rows (Employees, Attendance, LeaveManagement, Calendar, NoticeBoard, Jobs, Applications, ProcessLog, PayRoll), and the cartesian product of 36 `ModulePermission` rows |
| `2024-12-02-SeedMailTemplates` | [SeedMailTemplates.cs](../Codeji.CMS.Migrations/Migrations/SeedMailTemplates.cs) | Inserts one `MailTemplate` per value in the `EnumsHelper.MailType` enum (11 templates). Uses `[Placeholder]` syntax compatible with `HtmlTemplate.Render()` |
| `2025-02-04-AddPayrollSettingseAndItsModulePermissions` | [AddPayrollSettingseAndItsModulePermissions.cs](../Codeji.CMS.Migrations/Migrations/AddPayrollSettingseAndItsModulePermissions.cs) | Adds `Payroll_Settings` to `Module`, the matching `ModulePermission` rows, and back-fills `RolePermission` for each role of every existing company |
| `2026-01-01-AddPolicyModuleAndItsModulePermissions` | [AddPolicyModuleAndPermissions.cs](../Codeji.CMS.Migrations/Migrations/AddPolicyModuleAndPermissions.cs) | Same pattern — adds `Policy` module + permissions + back-fills role grants |

The two `Add…` migrations are the templates to copy when introducing a new module. The two `Seed…` migrations are reference for "global lookup data" insertion.

---

## Writing a new migration

### 1. Decide what kind it is

| Kind | When |
|---|---|
| **Schema-shape** (no schema, just convention) | Adding a new field to an entity that has a default value. **You usually don't need a migration** — MongoDB ignores missing fields, the C# property defaults handle it. Only write one if you need to populate the field for existing rows. |
| **Lookup seed** | Adding a fixed set of rows to a global lookup (Module, Permission, MailType template, etc.). |
| **Per-tenant backfill** | Adding rows for every existing company (e.g. new module → grant or deny on every role). The two `Add…` migrations above are this pattern. |
| **Data fix** | One-off correction (rename a value, fix a typo, recompute a denormalized count). Treat carefully — keep idempotent and small. |

### 2. Create the file

```
Codeji.CMS.Migrations/Migrations/2026-05-15-MyNewMigration.cs
```

(File name doesn't have to match the class name, but matching helps.)

### 3. Implement the class

```csharp
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public class MyNewMigration : IMigration
{
    public string Id => $"2026-05-15-{typeof(MyNewMigration).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        // … your changes …

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
```

### 4. Make it idempotent against partial state

If your migration adds rows, check whether each row already exists by some natural key before inserting. The runner-level skip (step 1 above) saves you from re-running on a fully-applied DB; the row-level check saves you from running on a partially-applied DB (someone added these rows manually before you got here).

Example pattern from `SeedBaseModulesAndPermissions`:

```csharp
var existingConstants = (await modules
    .Find(FilterDefinition<Module>.Empty)
    .Project(m => m.ModuleConstant)
    .ToListAsync()).ToHashSet();

foreach (var (constant, displayName) in BaseModules)
{
    if (existingConstants.Contains(constant)) continue;
    // insert
}
```

### 5. Test against a clone of prod

There's no rollback. Test against a recent restore.

### 6. Adding a new business module — the full chain

When you add a new module (say, `Assets`), the migration is one piece of a bigger change:

1. Add `Assets` to [`AppModule`](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs) constants.
2. Write a migration: insert `Module` row, the 4 `ModulePermission` rows, back-fill `RolePermission` for every existing role per company. (Use [`AddPolicyModuleAndPermissions.cs`](../Codeji.CMS.Migrations/Migrations/AddPolicyModuleAndPermissions.cs) as the template.)
3. Frontend `ModulesName` enum needs an `Assets = 'Assets'` entry — must match the `AppModule.Assets` string exactly.
4. Run the migration in every environment.

The migration ID order matters: this new migration's `Id` should sort *after* `2024-12-01-SeedBaseModulesAndPermissions` so the base modules and permissions exist when it runs.

---

## What the runner does NOT do

- **No transactions.** MongoDB supports them, but the runner doesn't open one. A migration that inserts 36 rows and then inserts a `Migration` record will leave the DB in a partial state if it fails between the two. Make every migration safe to re-run.
- **No rollback.** Write a separate "undo" migration if you need it.
- **No DI for services.** You only get `IMongoDatabase`. If you need to call into business services (e.g. send an email after backfill), do it from the API at startup, not from a migration.
- **No locking against concurrent runs.** Don't run `dotnet run` from two boxes at the same time.

---

## Migration output collection name

The runner writes to a collection literally named `Migration` (singular), not `Migrations`:

```csharp
var migrations = db.GetCollection<Migration>("Migration");
```

If you query manually for "what's been applied," use that exact name.

---

## Common mistakes

- **Forgetting the `Id` skip check.** Runner re-applies the migration on every run and you accumulate duplicates.
- **Forgetting the `Migration` insert.** Runner re-applies the migration on every run.
- **Using `DateTime.Now` instead of `DateTime.UtcNow`.** Migrations run in CI/dev/prod with different timezones — all timestamps in this codebase are UTC. Stay consistent.
- **Querying `_db.GetCollection<MyEntity>(typeof(MyEntity).Name)` and forgetting it bypasses repository tenant filtering.** That's intentional for migrations (you usually do want every tenant's rows), but don't reach for the same pattern outside the migrations project.
- **Hard-coding integer IDs that conflict with existing rows.** Always count what's there, increment, and stamp.
