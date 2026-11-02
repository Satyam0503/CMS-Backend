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

if (args.Contains("inspect-payroll-policy-profile-index", StringComparer.OrdinalIgnoreCase))
{
    var profiles = db.GetCollection<BsonDocument>("PayrollPolicyProfile");
    var duplicateGroups = await profiles.Aggregate()
        .Group(new BsonDocument
        {
            { "_id", new BsonDocument { { "CompanyId", "$CompanyId" }, { "Name", "$Name" }, { "EffectiveFrom", "$EffectiveFrom" } } },
            { "count", new BsonDocument("$sum", 1) }
        })
        .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
        .ToListAsync();
    using var indexCursor = await profiles.Indexes.ListAsync();
    var indexList = await indexCursor.ToListAsync();
    var repairId = new RepairPayrollPolicyProfileVersionIndex().Id;
    var migrationApplied = await db.GetCollection<BsonDocument>("Migration")
        .Find(Builders<BsonDocument>.Filter.Eq("_id", repairId)).AnyAsync();

    Console.WriteLine($"Database: {db.DatabaseNamespace.DatabaseName}");
    Console.WriteLine($"Profile count: {await profiles.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)}");
    Console.WriteLine($"Duplicate company/name/effective-from groups: {duplicateGroups.Count}");
    Console.WriteLine($"Legacy company/version index present: {indexList.Any(x => x.GetValue("name", "").AsString == "ux_policy_profile_company_version")}");
    Console.WriteLine($"Replacement company/name/start index present: {indexList.Any(x => x.GetValue("name", "").AsString == "ux_policy_profile_company_name_start")}");
    Console.WriteLine($"Repair migration recorded: {migrationApplied}");
    foreach (var duplicate in duplicateGroups)
        Console.WriteLine($"Duplicate key: {duplicate["_id"]}; count: {duplicate["count"]}");
    return;
}

if (args.Contains("repair-payroll-policy-profile-index", StringComparer.OrdinalIgnoreCase))
{
    var repair = new RepairPayrollPolicyProfileVersionIndex();
    await repair.ExecuteAsync(db);
    Console.WriteLine($"Applied targeted migration {repair.Id} to database {db.DatabaseNamespace.DatabaseName}.");
    return;
}

if (args.Contains("repair-wfh-outbox", StringComparer.OrdinalIgnoreCase))
{
    var repair = new RepairWorkFromHomeOutboxMissingRequestId();
    await repair.ExecuteAsync(db);
    Console.WriteLine($"Applied targeted migration {repair.Id} to database {db.DatabaseNamespace.DatabaseName}.");
    return;
}

if (args.Contains("inspect-wfh", StringComparer.OrdinalIgnoreCase))
{
    var requestsCol = db.GetCollection<BsonDocument>("WorkFromHomeRequest");
    var recentRequests = await requestsCol.Find(Builders<BsonDocument>.Filter.Empty)
        .Sort(Builders<BsonDocument>.Sort.Descending("CreatedDate"))
        .Limit(10)
        .ToListAsync();
    Console.WriteLine($"=== RECENT WFH REQUESTS ({recentRequests.Count}) ===");
    foreach (var r in recentRequests)
    {
        Console.WriteLine($"Id: {r.GetValue("RequestId", "")}, User: {r.GetValue("UserId", "")}, Emp: {r.GetValue("EmployeeId", "")}, Status: {r.GetValue("Status", "")}, Duration: {r.GetValue("DurationType", "")}, Reason: {r.GetValue("ReasonCode", "")}/{r.GetValue("ReasonText", "")}");
        Console.WriteLine($"   FromDate: {r.GetValue("FromDate", BsonNull.Value)} (BsonType: {r.GetValue("FromDate", BsonNull.Value).BsonType})");
        Console.WriteLine($"   ToDate: {r.GetValue("ToDate", BsonNull.Value)}");
        Console.WriteLine($"   CreatedDate: {r.GetValue("CreatedDate", BsonNull.Value)}");
    }

    var attCol = db.GetCollection<BsonDocument>("Attendance");
    var recentAtt = await attCol.Find(Builders<BsonDocument>.Filter.Gte("Date", new DateTime(2026, 10, 25)))
        .Sort(Builders<BsonDocument>.Sort.Descending("Date"))
        .Limit(20)
        .ToListAsync();
    Console.WriteLine($"=== ATTENDANCE RECORDS (Oct 25-31) ({recentAtt.Count}) ===");
    foreach (var a in recentAtt)
    {
        Console.WriteLine($"Id: {a.GetValue("AttendanceId", "")}, User: {a.GetValue("UserId", "")}, Date: {a.GetValue("Date", BsonNull.Value)}, Status: {a.GetValue("Status", "")}, Source: {a.GetValue("SourceType", "")}/{a.GetValue("SourceId", "")}, Remarks: {a.GetValue("Remarks", "")}");
    }

    var segCol = db.GetCollection<BsonDocument>("AttendanceDaySegment");
    var recentSeg = await segCol.Find(Builders<BsonDocument>.Filter.Gte("Date", new DateTime(2026, 10, 25)))
        .Sort(Builders<BsonDocument>.Sort.Descending("Date"))
        .Limit(20)
        .ToListAsync();
    Console.WriteLine($"=== SEGMENT RECORDS (Oct 25-31) ({recentSeg.Count}) ===");
    foreach (var s in recentSeg)
    {
        Console.WriteLine($"User: {s.GetValue("UserId", "")}, Date: {s.GetValue("Date", BsonNull.Value)}, Segment: {s.GetValue("Segment", "")}, Status: {s.GetValue("Status", "")}, Source: {s.GetValue("SourceType", "")}/{s.GetValue("SourceId", "")}");
    }
    return;
}

if (args.Contains("repair-wfh-dates", StringComparer.OrdinalIgnoreCase))
{
    var requestsCol = db.GetCollection<BsonDocument>("WorkFromHomeRequest");
    var segCol = db.GetCollection<BsonDocument>("AttendanceDaySegment");
    var attCol = db.GetCollection<BsonDocument>("Attendance");

    var allRequests = await requestsCol.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
    int fixedReqCount = 0;
    foreach (var r in allRequests)
    {
        bool changed = false;
        var id = r["_id"];
        if (r.TryGetValue("FromDate", out var fromVal) && fromVal.IsBsonDateTime)
        {
            var dt = fromVal.ToUniversalTime();
            if (dt.TimeOfDay != TimeSpan.Zero)
            {
                var correctedFrom = DateTime.SpecifyKind(dt.AddHours(5.5).Date, DateTimeKind.Utc);
                r["FromDate"] = new BsonDateTime(correctedFrom);
                changed = true;
            }
        }
        if (r.TryGetValue("ToDate", out var toVal) && toVal.IsBsonDateTime)
        {
            var dt = toVal.ToUniversalTime();
            if (dt.TimeOfDay != TimeSpan.Zero)
            {
                var correctedTo = DateTime.SpecifyKind(dt.AddHours(5.5).Date, DateTimeKind.Utc);
                r["ToDate"] = new BsonDateTime(correctedTo);
                changed = true;
            }
        }
        if (changed)
        {
            await requestsCol.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id), r);
            fixedReqCount++;
        }
    }
    Console.WriteLine($"Repaired {fixedReqCount} WorkFromHomeRequest documents with shifted dates.");

    var allSegments = await segCol.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
    int fixedSegCount = 0;
    foreach (var s in allSegments)
    {
        var id = s["_id"];
        if (s.TryGetValue("Date", out var dateVal) && dateVal.IsBsonDateTime)
        {
            var dt = dateVal.ToUniversalTime();
            if (dt.TimeOfDay != TimeSpan.Zero)
            {
                var correctedDate = DateTime.SpecifyKind(dt.AddHours(5.5).Date, DateTimeKind.Utc);
                s["Date"] = new BsonDateTime(correctedDate);
                await segCol.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id), s);
                fixedSegCount++;
            }
        }
    }
    Console.WriteLine($"Repaired {fixedSegCount} AttendanceDaySegment documents with shifted dates.");

    var aaravUser = "0e94eb40-53ee-42ea-8598-f3c8edf02863";
    var oct28Utc = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);
    var aaravSegmentsOct28 = await segCol.Find(Builders<BsonDocument>.Filter.And(
        Builders<BsonDocument>.Filter.Eq("UserId", aaravUser),
        Builders<BsonDocument>.Filter.Eq("Date", oct28Utc)
    )).ToListAsync();

    if (aaravSegmentsOct28.Count >= 2)
    {
        var fh = aaravSegmentsOct28.FirstOrDefault(x => x.GetValue("Segment", "").AsString == "FIRST_HALF");
        var sh = aaravSegmentsOct28.FirstOrDefault(x => x.GetValue("Segment", "").AsString == "SECOND_HALF");
        if (fh != null && sh != null)
        {
            string status = "WFH+WFO";
            var update = Builders<BsonDocument>.Update
                .Set("Status", status)
                .Set("RemarkCode", "WFH_WFO_COMBINATION")
                .Set("Remarks", "Administrative WFH grant: First Half WFH + Second Half Office");
            await attCol.UpdateOneAsync(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("UserId", aaravUser),
                Builders<BsonDocument>.Filter.Eq("Date", oct28Utc)
            ), update);
            Console.WriteLine($"Synchronized Aarav Sharma daily attendance on 2026-10-28 to {status}.");
        }
    }
    return;
}

if (args.Contains("inspect-lifecycle-policies", StringComparer.OrdinalIgnoreCase))
{
    var companies = db.GetCollection<BsonDocument>("Company");
    var employees = db.GetCollection<BsonDocument>("Employee");
    var lifecyclePolicies = db.GetCollection<BsonDocument>("EmployeeLifecyclePolicy");
    var active = await lifecyclePolicies.CountDocumentsAsync(new BsonDocument { { "IsDeleted", false }, { "IsActive", true } });
    var duplicateGroups = await lifecyclePolicies.Aggregate()
        .Match(new BsonDocument { { "IsDeleted", false }, { "IsActive", true } })
        .Group(new BsonDocument { { "_id", new BsonDocument { { "CompanyId", "$CompanyId" }, { "EventType", "$EventType" } } }, { "count", new BsonDocument("$sum", 1) } })
        .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
        .Count()
        .FirstOrDefaultAsync();
    var byEvent = await lifecyclePolicies.Aggregate()
        .Match(new BsonDocument { { "IsDeleted", false }, { "IsActive", true } })
        .Group(new BsonDocument { { "_id", "$EventType" }, { "count", new BsonDocument("$sum", 1) } })
        .Sort(new BsonDocument("_id", 1))
        .ToListAsync();
    var companyCount = await companies.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    var nonDeletedCompanyCount = await companies.CountDocumentsAsync(Builders<BsonDocument>.Filter.Ne("IsDeleted", true));
    var employeeCompanyIds = await employees.Distinct<string>("CompanyId", FilterDefinition<BsonDocument>.Empty).ToListAsync();
    var sampleCompanyFields = (await companies.Find(FilterDefinition<BsonDocument>.Empty).Limit(1).ToListAsync()).FirstOrDefault()?.Names.ToArray() ?? Array.Empty<string>();
    var sampleCompany = (await companies.Find(FilterDefinition<BsonDocument>.Empty).Limit(1).ToListAsync()).FirstOrDefault();
    var sampleCompanyId = sampleCompany?.GetValue("_id", BsonNull.Value);
    Console.WriteLine($"{{\"activePolicies\":{active},\"duplicateActiveCompanyEventGroups\":{duplicateGroups?.Count ?? 0},\"eventCounts\":{byEvent.ToJson()},\"companyCount\":{companyCount},\"nonDeletedCompanyCount\":{nonDeletedCompanyCount},\"employeeCompanyCount\":{employeeCompanyIds.Count},\"sampleCompanyFields\":{System.Text.Json.JsonSerializer.Serialize(sampleCompanyFields)},\"sampleCompanyIdType\":\"{sampleCompanyId?.BsonType}\",\"sampleCompanyId\":\"{sampleCompanyId}\"}}");
    return;
}

if (args.Contains("apply-lifecycle-policies", StringComparer.OrdinalIgnoreCase))
{
    var lifecycleMigrationIds = new[]
    {
        "2026-09-08-01-SeedEmployeeLifecyclePolicies",
        "2026-09-08-02-SeedEmployeeLeaveLifecyclePolicies",
        "2026-09-08-03-SeedWorkingLifecyclePolicy",
        "2026-09-08-04-CorrectAbscondingLifecyclePolicy",
        "2026-09-08-05-AddEmployeeLifecyclePolicyIndexes",
    };
    await db.GetCollection<BsonDocument>("Migration").DeleteManyAsync(
        Builders<BsonDocument>.Filter.In("_id", lifecycleMigrationIds));
    await new SeedEmployeeLifecyclePolicies().ExecuteAsync(db);
    await new SeedEmployeeLeaveLifecyclePolicies().ExecuteAsync(db);
    await new SeedWorkingLifecyclePolicy().ExecuteAsync(db);
    await new CorrectAbscondingLifecyclePolicy().ExecuteAsync(db);
    await new AddEmployeeLifecyclePolicyIndexes().ExecuteAsync(db);
    await new SeedCompanyNotificationDefaults().ExecuteAsync(db);
    Console.WriteLine("Lifecycle policy migrations applied.");
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

// Deployments must identify their slot explicitly. ASPNETCORE_ENVIRONMENT is
// not sufficient because both Dev and UAT run the API as Development.
var targetArguments = args.Select((value, index) => (value, index))
    .Where(item => item.value.Equals("--deployment-target", StringComparison.OrdinalIgnoreCase))
    .ToArray();
var validTargetNames = Enum.GetNames<MigrationTarget>();
if (targetArguments.Length != 1 || targetArguments[0].index + 1 >= args.Length ||
    !validTargetNames.Contains(args[targetArguments[0].index + 1], StringComparer.OrdinalIgnoreCase) ||
    !Enum.TryParse<MigrationTarget>(args[targetArguments[0].index + 1], true, out var migrationTarget))
{
    throw new InvalidOperationException("Specify --deployment-target Development, Uat, or Production. Dev/UAT targets withhold data backfills.");
}

// Load migrations dynamically
var migrations = MigrationLoader.LoadMigrations();

var runner = new MigrationRunner(migrations, db, migrationTarget);
await runner.RunAsync();
