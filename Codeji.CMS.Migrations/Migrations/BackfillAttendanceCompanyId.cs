using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Repairs legacy/test attendance documents created before attendance queries
/// became company-scoped. UserId is the authoritative mapping; EmployeeId is
/// used only when it uniquely identifies one company.
/// </summary>
public sealed class BackfillAttendanceCompanyId : IMigration
{
    public string Id => $"2026-07-22-04-{typeof(BackfillAttendanceCompanyId).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var employees = db.GetCollection<EmpUser>(typeof(EmpUser).Name);
        var missingCompany = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("CompanyId", false),
            Builders<BsonDocument>.Filter.Eq("CompanyId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("CompanyId", ""));
        var records = await attendance.Find(missingCompany).ToListAsync();

        var userIds = records.Where(x => x.TryGetValue("UserId", out var value) && value.IsString)
            .Select(x => x["UserId"].AsString).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var employeeIds = records.Where(x => x.TryGetValue("EmployeeId", out var value) && value.IsString)
            .Select(x => x["EmployeeId"].AsString).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var employeeRows = await employees.Find(e => userIds.Contains(e.UserId) || employeeIds.Contains(e.EmployeeId)).ToListAsync();
        var companyByUser = employeeRows.Where(e => !string.IsNullOrWhiteSpace(e.UserId) && !string.IsNullOrWhiteSpace(e.CompanyId))
            .GroupBy(e => e.UserId).ToDictionary(g => g.Key, g => g.First().CompanyId);
        var companyByUniqueEmployeeId = employeeRows.Where(e => !string.IsNullOrWhiteSpace(e.EmployeeId) && !string.IsNullOrWhiteSpace(e.CompanyId))
            .GroupBy(e => e.EmployeeId)
            .Where(g => g.Select(e => e.CompanyId).Distinct().Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().CompanyId);

        foreach (var record in records)
        {
            string? companyId = null;
            if (record.TryGetValue("UserId", out var userValue) && userValue.IsString)
                companyByUser.TryGetValue(userValue.AsString, out companyId);
            if (string.IsNullOrWhiteSpace(companyId) && record.TryGetValue("EmployeeId", out var employeeValue) && employeeValue.IsString)
                companyByUniqueEmployeeId.TryGetValue(employeeValue.AsString, out companyId);
            if (string.IsNullOrWhiteSpace(companyId)) continue;

            await attendance.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", record["_id"]),
                Builders<BsonDocument>.Update.Set("CompanyId", companyId));
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
