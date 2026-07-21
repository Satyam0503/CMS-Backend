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

        await CreateUnique(db, "AttendancePayrollException", "ux_exception_company_employee_month_type", "CompanyId", "EmployeeId", "PayrollMonth", "ExceptionType");
        await CreateUnique(db, "MonthlyAttendanceSummary", "ux_summary_company_employee_month", "CompanyId", "EmployeeId", "PayrollMonth");
        await CreateUnique(db, "AttendancePenaltyPolicy", "ux_policy_company_version", "CompanyId", "Version");
        await CreateUnique(db, "PayrollDivisorPolicy", "ux_divisor_company_version", "CompanyId", "Version");
        await CreateUnique(db, "EmpPayRoll", "ux_payroll_company_employee_month", "CompanyId", "EmployeeId", "PayMonth");
        await CreateUnique(db, "Attendance", "ux_attendance_user_date", "UserId", "Date");

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task CreateUnique(IMongoDatabase db, string collectionName, string indexName, params string[] fields)
    {
        var keys = new BsonDocument(fields.Select(x => new BsonElement(x, 1)));
        try
        {
            await db.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = indexName, Unique = true }));
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            // Do not make the entire migration runner unusable because unrelated
            // legacy data needs a separate, domain-specific consolidation decision.
            Console.WriteLine($"Skipped {indexName}: {collectionName} contains legacy duplicate keys.");
        }
    }
}
