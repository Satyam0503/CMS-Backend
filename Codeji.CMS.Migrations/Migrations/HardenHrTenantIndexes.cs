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
            Builders<BsonDocument>.Filter.Eq("CompanyId", ""))).ToListAsync();
        foreach (var row in missing)
        {
            if (!row.TryGetValue("UserId", out var userId) || userId.IsBsonNull) continue;
            var employee = await employees.Find(Builders<BsonDocument>.Filter.Eq("UserId", userId)).FirstOrDefaultAsync();
            if (employee == null || !employee.TryGetValue("CompanyId", out var companyId)) continue;
            await attendance.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update.Set("CompanyId", companyId));
        }
    }

    private static async Task BackfillSalaryCompany(IMongoDatabase db)
    {
        var salaries = db.GetCollection<BsonDocument>("SalaryModel");
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var missing = await salaries.Find(Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("CompanyId", false),
            Builders<BsonDocument>.Filter.Eq("CompanyId", ""))).ToListAsync();
        foreach (var row in missing)
        {
            BsonDocument? employee = null;
            if (row.TryGetValue("EmployeeId", out var employeeId) && !employeeId.IsBsonNull)
                employee = await employees.Find(Builders<BsonDocument>.Filter.Eq("EmployeeId", employeeId)).FirstOrDefaultAsync();
            if (employee == null || !employee.TryGetValue("CompanyId", out var companyId)) continue;
            await salaries.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update.Set("CompanyId", companyId));
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

    private static Task CreateUnique(IMongoDatabase db, string collection, string name, params string[] fields) =>
        db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument(fields.Select(x => new BsonElement(x, 1))),
            new CreateIndexOptions { Name = name, Unique = true }));
}
