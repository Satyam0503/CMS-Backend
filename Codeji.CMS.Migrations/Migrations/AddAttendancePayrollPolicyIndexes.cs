using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public class AddAttendancePayrollPolicyIndexes : IMigration
{
    public string Id => $"2026-07-21-{typeof(AddAttendancePayrollPolicyIndexes).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var completed = true;
        completed &= await CreateUnique(db, "AttendancePayrollException", "ux_exception_company_employee_month_type", "CompanyId", "EmployeeId", "PayrollMonth", "ExceptionType");
        completed &= await CreateUnique(db, "MonthlyAttendanceSummary", "ux_summary_company_employee_month", "CompanyId", "EmployeeId", "PayrollMonth");
        completed &= await CreateUnique(db, "AttendancePenaltyPolicy", "ux_policy_company_version", "CompanyId", "Version");
        completed &= await CreateUnique(db, "PayrollDivisorPolicy", "ux_divisor_company_version", "CompanyId", "Version");
        completed &= await CreateUnique(db, "EmpPayRoll", "ux_payroll_company_employee_month", "CompanyId", "EmployeeId", "PayMonth");
        completed &= await CreateUnique(db, "Attendance", "ux_attendance_user_date", "UserId", "Date");

        // A duplicate-key conflict means the unique index could not be created.
        // Do not mark this migration complete; after legacy duplicates are repaired,
        // the migration must be retried so tenant/payroll uniqueness is enforced.
        if (completed)
            await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task<bool> CreateUnique(IMongoDatabase db, string collectionName, string indexName, params string[] fields)
    {
        var collection = db.GetCollection<BsonDocument>(collectionName);
        var existing = await collection.Indexes.ListAsync();
        var existingIndexes = await existing.ToListAsync();
        var keys = new BsonDocument(fields.Select(x => new BsonElement(x, 1)));
        var matchingIndex = existingIndexes.FirstOrDefault(index =>
            index.TryGetValue("name", out var name) && name.IsString && name.AsString == indexName);
        if (matchingIndex is not null)
        {
            var isUnique = matchingIndex.TryGetValue("unique", out var unique) && unique.IsBoolean && unique.AsBoolean;
            var hasExpectedKeys = matchingIndex.TryGetValue("key", out var existingKeys)
                && existingKeys.IsBsonDocument
                && existingKeys.AsBsonDocument.ToJson() == keys.ToJson();
            if (isUnique && hasExpectedKeys) return true;
            throw new InvalidOperationException($"Index {indexName} exists with an incompatible definition.");
        }

        try
        {
            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = indexName, Unique = true }));
            return true;
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            Console.WriteLine($"Skipped {indexName}: {collectionName} contains legacy duplicate keys; migration will retry later.");
            return false;
        }
    }
}
