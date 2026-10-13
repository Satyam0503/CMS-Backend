using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Backfills tenant keys introduced by the HR hardening work and replaces
/// legacy identity indexes with tenant-aware unique indexes.
/// </summary>
public sealed class HardenHrTenantIndexes : IMigration
{
    public string Id => $"2026-07-22-{typeof(HardenHrTenantIndexes).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await BackfillAttendanceCompany(db);
        await BackfillSalaryCompany(db);
        await NormalizeLeavePolicies(db);
        await SeedAttendanceStatuses(db);

        await DropIfExists(db, "Attendance", "ux_attendance_user_date");
        await CreateUnique(db, "Attendance", "ux_attendance_company_user_date", "CompanyId", "UserId", "Date");
        await CreateUnique(db, "LeavePolicy", "ux_leave_policy_company_name", "CompanyId", "NormalizedName");
        await CreateUnique(db, "LeavePolicy", "ux_leave_policy_company_code", "CompanyId", "NormalizedCode");
        await CreateUnique(db, "EmployeeLeaveBalance", "ux_leave_balance_company_user_policy", "CompanyId", "UserId", "LeavePolicyId");
        await CreateUnique(db, "AttendanceStatusSetting", "ux_attendance_status_company_code", "CompanyId", "Code");
        await CreateUnique(db, "SalaryModel", "ux_salary_company_user_effective", "CompanyId", "UserId", "EffectiveFrom");

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task BackfillAttendanceCompany(IMongoDatabase db)
    {
        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var missing = await attendance.Find(Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("CompanyId", false),
            Builders<BsonDocument>.Filter.Eq("CompanyId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("CompanyId", ""),
            Builders<BsonDocument>.Filter.Type("CompanyId", BsonType.Null))).ToListAsync();
        foreach (var row in missing)
        {
            if (!row.TryGetValue("UserId", out var userId) || userId.IsBsonNull) continue;
            var userStr = userId.IsString ? userId.AsString : userId.ToString();
            if (string.IsNullOrWhiteSpace(userStr)) continue;
            var employee = await employees.Find(Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("UserId", userStr),
                Builders<BsonDocument>.Filter.Eq("_id", userStr))).FirstOrDefaultAsync();
            if (employee == null || !employee.TryGetValue("CompanyId", out var companyId) || companyId.IsBsonNull || string.IsNullOrWhiteSpace(companyId.ToString())) continue;
            await attendance.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update.Set("CompanyId", companyId.ToString()));
        }
    }

    private static async Task BackfillSalaryCompany(IMongoDatabase db)
    {
        var salaries = db.GetCollection<BsonDocument>("SalaryModel");
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var missingFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("CompanyId", false),
            Builders<BsonDocument>.Filter.Eq("CompanyId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("CompanyId", ""),
            Builders<BsonDocument>.Filter.Type("CompanyId", BsonType.Null));

        var missing = await salaries.Find(missingFilter).ToListAsync();
        foreach (var row in missing)
        {
            BsonDocument? employee = null;
            if (row.TryGetValue("UserId", out var userId) && !userId.IsBsonNull)
            {
                var userStr = userId.IsString ? userId.AsString : userId.ToString();
                if (!string.IsNullOrWhiteSpace(userStr))
                {
                    employee = await employees.Find(Builders<BsonDocument>.Filter.Or(
                        Builders<BsonDocument>.Filter.Eq("UserId", userStr),
                        Builders<BsonDocument>.Filter.Eq("_id", userStr))).FirstOrDefaultAsync();
                }
            }

            if (employee == null && row.TryGetValue("EmployeeId", out var employeeId) && !employeeId.IsBsonNull)
            {
                var empIdStr = employeeId.IsString ? employeeId.AsString : employeeId.ToString();
                if (!string.IsNullOrWhiteSpace(empIdStr))
                {
                    employee = await employees.Find(Builders<BsonDocument>.Filter.Eq("EmployeeId", empIdStr)).FirstOrDefaultAsync();
                }
            }

            if (employee != null && employee.TryGetValue("CompanyId", out var companyId) && !companyId.IsBsonNull && !string.IsNullOrWhiteSpace(companyId.ToString()))
            {
                var updates = new List<UpdateDefinition<BsonDocument>>
                {
                    Builders<BsonDocument>.Update.Set("CompanyId", companyId.ToString())
                };

                if ((!row.TryGetValue("UserId", out var curUser) || curUser.IsBsonNull || string.IsNullOrWhiteSpace(curUser.ToString())) &&
                    employee.TryGetValue("UserId", out var empUserId) && !empUserId.IsBsonNull)
                {
                    updates.Add(Builders<BsonDocument>.Update.Set("UserId", empUserId.ToString()));
                }

                if ((!row.TryGetValue("EmployeeId", out var curEmp) || curEmp.IsBsonNull || string.IsNullOrWhiteSpace(curEmp.ToString())) &&
                    employee.TryGetValue("EmployeeId", out var empEmpId) && !empEmpId.IsBsonNull)
                {
                    updates.Add(Builders<BsonDocument>.Update.Set("EmployeeId", empEmpId.ToString()));
                }

                await salaries.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                    Builders<BsonDocument>.Update.Combine(updates));
            }
        }

        // Clean up orphaned records that have no valid CompanyId or UserId
        var orphanFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("CompanyId", false),
            Builders<BsonDocument>.Filter.Eq("CompanyId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("CompanyId", ""),
            Builders<BsonDocument>.Filter.Type("CompanyId", BsonType.Null),
            Builders<BsonDocument>.Filter.Exists("UserId", false),
            Builders<BsonDocument>.Filter.Eq("UserId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("UserId", ""));

        await salaries.DeleteManyAsync(orphanFilter);

        // Deduplicate records sharing the same (CompanyId, UserId, EffectiveFrom)
        await DeduplicateSalaries(db);
    }

    private static async Task DeduplicateSalaries(IMongoDatabase db)
    {
        var salaries = db.GetCollection<BsonDocument>("SalaryModel");
        var allSalaries = await salaries.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        var duplicateGroups = allSalaries
            .GroupBy(row => new
            {
                CompanyId = row.TryGetValue("CompanyId", out var c) && !c.IsBsonNull ? c.ToString() : string.Empty,
                UserId = row.TryGetValue("UserId", out var u) && !u.IsBsonNull ? u.ToString() : string.Empty,
                EffectiveFrom = row.TryGetValue("EffectiveFrom", out var ef) && ef.IsBsonDateTime ? ef.AsBsonDateTime.MillisecondsSinceEpoch : 0L
            })
            .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key.CompanyId) && !string.IsNullOrWhiteSpace(g.Key.UserId))
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var docs = group.ToList();
            var keeper = docs
                .OrderByDescending(d => d.TryGetValue("Status", out var s) && !s.IsBsonNull && s.ToBoolean())
                .ThenByDescending(d => d.TryGetValue("GrossSalary", out var gs) && !gs.IsBsonNull && gs.ToDecimal() > 0)
                .ThenByDescending(d => d.TryGetValue("UpdatedAt", out var u) && u.IsValidDateTime ? u.ToUniversalTime() : DateTime.MinValue)
                .ThenByDescending(d => d.TryGetValue("CreatedAt", out var cr) && cr.IsValidDateTime ? cr.ToUniversalTime() : DateTime.MinValue)
                .ThenByDescending(d => d["_id"].ToString(), StringComparer.Ordinal)
                .First();

            var removeIds = docs.Where(d => d["_id"] != keeper["_id"]).Select(d => d["_id"]).ToList();
            if (removeIds.Count > 0)
            {
                await salaries.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", removeIds));
            }
        }
    }

    private static async Task NormalizeLeavePolicies(IMongoDatabase db)
    {
        var policies = db.GetCollection<BsonDocument>("LeavePolicy");
        var rows = await policies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        foreach (var row in rows)
        {
            var name = row.TryGetValue("Name", out var n) && n.IsString ? n.AsString.Trim() : string.Empty;
            var code = row.TryGetValue("Code", out var c) && c.IsString ? c.AsString.Trim() : string.Empty;
            await policies.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update
                    .Set("Name", name)
                    .Set("Code", code.ToUpperInvariant())
                    .Set("NormalizedName", name.ToUpperInvariant())
                    .Set("NormalizedCode", code.ToUpperInvariant()));
        }
    }

    private static async Task SeedAttendanceStatuses(IMongoDatabase db)
    {
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var settings = db.GetCollection<BsonDocument>("AttendanceStatusSetting");
        var companies = await employees.Distinct<string>("CompanyId", Builders<BsonDocument>.Filter.Ne("CompanyId", "")).ToListAsync();
        var defaults = new (string Code, string Name, bool Time, decimal Paid, decimal Unpaid)[]
        {
            ("P","Present",true,1,0), ("A","Absent",false,0,1), ("SL","Sick Leave",false,1,0),
            ("CL","Casual Leave",false,1,0), ("EL","Earned Leave",false,1,0), ("WFH","Work From Home",true,1,0),
            ("HD","Half Day",true,.5m,.5m), ("ED","Early Departure",true,1,0), ("LHD","Late Arrival-Half Day",true,.5m,.5m),
            ("WFH+WFO","Half WFH and Half WFO",true,1,0), ("COMP-OFF","Compensatory Off",false,1,0),
            ("CL-HALF","Casual Leave (Half Day)",false,.5m,0), ("SL-HALF","Sick Leave (Half Day)",false,.5m,0),
            ("WFH-HD","WFH with Half Day",true,.5m,.5m)
        };
        foreach (var company in companies)
        for (var i = 0; i < defaults.Length; i++)
        {
            var item = defaults[i];
            var filter = Builders<BsonDocument>.Filter.Eq("CompanyId", company) & Builders<BsonDocument>.Filter.Eq("Code", item.Code);
            var update = Builders<BsonDocument>.Update
                .SetOnInsert("_id", Guid.NewGuid().ToString()).SetOnInsert("CompanyId", company).SetOnInsert("Code", item.Code)
                .SetOnInsert("Name", item.Name).SetOnInsert("IsActive", true).SetOnInsert("IsSystem", true)
                .SetOnInsert("SortOrder", i).SetOnInsert("RequiresTime", item.Time)
                .SetOnInsert("PaidDayFraction", item.Paid).SetOnInsert("UnpaidDayFraction", item.Unpaid);
            await settings.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }
    }

    private static async Task DropIfExists(IMongoDatabase db, string collection, string index)
    {
        try { await db.GetCollection<BsonDocument>(collection).Indexes.DropOneAsync(index); }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexNotFound") { }
    }

    private static async Task CreateUnique(IMongoDatabase db, string collection, string name, params string[] fields)
    {
        var col = db.GetCollection<BsonDocument>(collection);
        using var cursor = await col.Indexes.ListAsync();
        var existing = await cursor.ToListAsync();
        if (existing.Any(i => i.GetValue("name", string.Empty).AsString == name))
        {
            return;
        }

        var keys = new BsonDocument(fields.Select(x => new BsonElement(x, 1)));
        try
        {
            await col.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                keys,
                new CreateIndexOptions { Name = name, Unique = true }));
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            Console.WriteLine($"Skipped {name} on {collection}: duplicate keys found ({ex.Message}).");
        }
    }
}
