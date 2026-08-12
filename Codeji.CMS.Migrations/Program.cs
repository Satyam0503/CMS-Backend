// See https://aka.ms/new-console-template for more information
using System.Reflection;
using Codeji.CMS.GenericRepository.Settings;
using Codeji.CMS.Migrations;
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Migrations.Migrations;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

// connect to database
var connection = configuration.GetConnectionString("mongodb")
    ?? throw new InvalidOperationException("ConnectionStrings:mongodb is not configured.");
var mongoUrl = MongoUrl.Create(connection);
var mongodb = new MongoClient(connection);
var db = mongodb.GetDatabase(mongoUrl.DatabaseName);

if (args.Contains("inspect-attendance", StringComparer.OrdinalIgnoreCase))
{
    var attendance = db.GetCollection<BsonDocument>("Attendance");
    var total = await attendance.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
    var attendanceIdFieldDocs = await attendance.CountDocumentsAsync(Builders<BsonDocument>.Filter.Exists("AttendanceId", true));
    var nullIdDocs = await attendance.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("_id", BsonNull.Value));
    var nonObjectIdIdDocs = await attendance.CountDocumentsAsync(Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type("_id", BsonType.ObjectId)));
    var julyFilter = Builders<BsonDocument>.Filter.And(
        Builders<BsonDocument>.Filter.Gte("Date", new DateTime(2026, 7, 1)),
        Builders<BsonDocument>.Filter.Lt("Date", new DateTime(2026, 8, 1))
    );
    var julyCount = await attendance.CountDocumentsAsync(julyFilter);
    var duplicateCompanyUserDate = await attendance.Aggregate()
        .Group(new BsonDocument
        {
            { "_id", new BsonDocument { { "CompanyId", "$CompanyId" }, { "UserId", "$UserId" }, { "Date", "$Date" } } },
            { "count", new BsonDocument("$sum", 1) }
        })
        .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
        .Count()
        .FirstOrDefaultAsync();

    Console.WriteLine($"Attendance total docs: {total}");
    Console.WriteLine($"AttendanceId field present explicitly: {attendanceIdFieldDocs}");
    Console.WriteLine($"Docs with null _id: {nullIdDocs}");
    Console.WriteLine($"Docs with invalid _id type (non-ObjectId): {nonObjectIdIdDocs}");
    Console.WriteLine($"July 2026 attendance docs: {julyCount}");
    Console.WriteLine($"Duplicate company/user/date groups: {duplicateCompanyUserDate}");
    return;
}

if (args.Contains("repair-attendance-null-ids", StringComparer.OrdinalIgnoreCase))
{
    var attendance = db.GetCollection<BsonDocument>("Attendance");
    var nullDocs = await attendance.Find(Builders<BsonDocument>.Filter.Eq("_id", BsonNull.Value)).ToListAsync();
    Console.WriteLine($"Found {nullDocs.Count} attendance docs with null _id.");
    var fixedCount = 0;
    foreach (var doc in nullDocs)
    {
        var companyId = doc.GetValue("CompanyId", BsonNull.Value);
        var userId = doc.GetValue("UserId", BsonNull.Value);
        var date = doc.GetValue("Date", BsonNull.Value);
        Console.WriteLine($"Null-id attendance doc details: CompanyId={companyId}, UserId={userId}, Date={date}, _id={doc.GetValue("_id", BsonNull.Value)}");
        if (companyId.IsBsonNull || userId.IsBsonNull || date.IsBsonNull)
        {
            Console.WriteLine("Skipping invalid attendance doc with missing key fields.");
            await attendance.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", BsonNull.Value));
            continue;
        }

        var canonicalFilter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("CompanyId", companyId),
            Builders<BsonDocument>.Filter.Eq("UserId", userId),
            Builders<BsonDocument>.Filter.Eq("Date", date)
        );
        var duplicateCount = await attendance.CountDocumentsAsync(canonicalFilter);
        if (duplicateCount > 1)
        {
            Console.WriteLine($"Found duplicate attendance docs for CompanyId/UserId/Date; deleting null _id doc.");
            await attendance.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", BsonNull.Value));
            fixedCount++;
            continue;
        }

        await attendance.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", BsonNull.Value));
        if (duplicateCount == 1)
        {
            Console.WriteLine($"Canonical attendance already exists for CompanyId/UserId/Date; removed null _id duplicate.");
            fixedCount++;
            continue;
        }

        doc.Remove("_id");
        doc["_id"] = ObjectId.GenerateNewId();
        await attendance.InsertOneAsync(doc);
        fixedCount++;
    }
    Console.WriteLine($"Fixed {fixedCount} attendance docs with null _id.");
    return;
}

// Targeted operational migrations are intentionally opt-in. This avoids running
// unrelated historical migrations when HR needs to repair leave eligibility.
if (args.Contains("migrate-leave-balance-allocation-fields", StringComparer.OrdinalIgnoreCase))
{
    await new MigrateEmployeeLeaveBalanceAllocationFields().ExecuteAsync(db);
    return;
}

if (args.Contains("deduplicate-employee-leave-balances", StringComparer.OrdinalIgnoreCase))
{
    await new DeduplicateEmployeeLeaveBalanceRecords().ExecuteAsync(db);
    return;
}

if (args.Contains("reassign-active-leave-policies", StringComparer.OrdinalIgnoreCase))
{
    await new ReassignActiveLeavePoliciesToAllEmployees().ExecuteAsync(db);
    return;
}

if (args.Contains("correct-leave-policy-tenant-scope", StringComparer.OrdinalIgnoreCase))
{
    await new CorrectLeavePolicyAssignmentTenantScope().ExecuteAsync(db);
    return;
}

if (args.Contains("inspect-leave-policy-assignments", StringComparer.OrdinalIgnoreCase))
{
    var employees = await db.GetCollection<BsonDocument>("EmpUser").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
    var employeeCompanyByUserId = employees
        .Where(employee => employee.GetValue("_id", BsonNull.Value).IsString && employee.GetValue("CompanyId", BsonNull.Value).IsString)
        .ToDictionary(employee => employee["_id"].AsString, employee => employee["CompanyId"].AsString, StringComparer.Ordinal);
    var balances = await db.GetCollection<BsonDocument>("EmployeeLeaveBalance").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
    var crossTenant = balances.Count(balance =>
        balance.GetValue("UserId", BsonNull.Value).IsString && balance.GetValue("CompanyId", BsonNull.Value).IsString &&
        employeeCompanyByUserId.TryGetValue(balance["UserId"].AsString, out var employeeCompany) && employeeCompany != balance["CompanyId"].AsString);
    var missingAllocationFields = balances.Count(balance =>
        !balance.Contains("TotalAllocated") || !balance.Contains("Taken") || !balance.Contains("Remaining"));
    var legacyBalanceFields = balances.Count(balance => balance.Contains("Balance") || balance.Contains("UsedBalance"));
    var manualAllocations = balances.Count(balance => balance.GetValue("IsManualAllocation", false).ToBoolean());
    Console.WriteLine($"{{\"employeeLeaveBalances\":{balances.Count},\"crossTenantBalances\":{crossTenant},\"balancesMissingAllocationFields\":{missingAllocationFields},\"balancesWithLegacyFields\":{legacyBalanceFields},\"manualAllocations\":{manualAllocations}}}");
    return;
}

if (args.Contains("remove-legacy-leave-balance-fields", StringComparer.OrdinalIgnoreCase))
{
    await new RemoveLegacyEmployeeLeaveBalanceFields().ExecuteAsync(db);
    return;
}

if (args.Contains("preserve-manual-leave-allocations", StringComparer.OrdinalIgnoreCase))
{
    await new PreserveDistinctManualLeaveAllocations().ExecuteAsync(db);
    return;
}

if (args.Contains("deduplicate-company-roles", StringComparer.OrdinalIgnoreCase))
{
    await new DeduplicateCompanyRoles().ExecuteAsync(db);
    return;
}

if (args.Contains("inspect-tenant-role-attendance-data", StringComparer.OrdinalIgnoreCase))
{
    var companyRoles = await db.GetCollection<BsonDocument>("Roles").Find(
        Builders<BsonDocument>.Filter.Gt("CompanyId", "")).ToListAsync();
    var duplicateRoles = companyRoles.GroupBy(role => (role.GetValue("CompanyId", "").AsString, role.GetValue("RoleType", 0).ToInt32()))
        .Count(group => group.Count() > 1);
    var statusRows = await db.GetCollection<BsonDocument>("AttendanceStatusSetting").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
    var duplicateStatuses = statusRows.GroupBy(status => (status.GetValue("CompanyId", "").AsString, status.GetValue("Code", "").AsString))
        .Count(group => !string.IsNullOrWhiteSpace(group.Key.Item1) && group.Count() > 1);
    var statusesWithoutCompany = statusRows.Count(status => string.IsNullOrWhiteSpace(status.GetValue("CompanyId", "").AsString));
    Console.WriteLine($"{{\"duplicateTenantRoleGroups\":{duplicateRoles},\"duplicateTenantAttendanceStatusGroups\":{duplicateStatuses},\"attendanceStatusesWithoutCompany\":{statusesWithoutCompany}}}");
    return;
}

// Load migrations dynamically
var migrations = MigrationLoader.LoadMigrations();

var runner = new MigrationRunner(migrations, db);
await runner.RunAsync();
