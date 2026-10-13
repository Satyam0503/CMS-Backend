using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class BackfillLegacyEmployeeSalaryStructures : IMigration
{
    public string Id => $"2026-09-30-{nameof(BackfillLegacyEmployeeSalaryStructures)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var legacy = db.GetCollection<BsonDocument>("SalaryModel");
        var canonical = db.GetCollection<BsonDocument>("EmployeeSalaryStructure");
        using var cursor = await legacy.FindAsync(Builders<BsonDocument>.Filter.Empty);

        while (await cursor.MoveNextAsync())
        {
            foreach (var source in cursor.Current)
            {
                var companyId = ReadString(source, "CompanyId");
                var employeeId = ReadString(source, "EmployeeId");
                var sourceId = source.GetValue("_id", BsonNull.Value);
                if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(employeeId) || sourceId.IsBsonNull) continue;

                var exists = await canonical.Find(Builders<BsonDocument>.Filter.Eq("_id", sourceId)).AnyAsync();
                if (exists) continue;

                var active = ReadBoolean(source, "Status", true);
                var effectiveFrom = ReadDate(source, "EffectiveFrom", DateTime.UtcNow).Date;
                var effectiveTo = ReadNullableDate(source, "EffectiveTo");
                var components = new BsonArray
                {
                    Component("BASIC", "Basic Salary", ReadDecimal(source, "BasicPay"), "EARNING", true),
                    Component("HRA", "House Rent Allowance", ReadDecimal(source, "Hra"), "EARNING", true),
                    Component("LTA", "Leave Travel Allowance", ReadDecimal(source, "Lta"), "EARNING", true),
                    Component("OTHER_ALLOWANCE", "Other Allowance", ReadDecimal(source, "OtherAllowances"), "EARNING", true),
                    Component("BONUS", "Bonus", ReadDecimal(source, "Bonus"), "EARNING", true),
                    Component("HEALTH_INSURANCE", "Health Insurance", ReadDecimal(source, "HealthInsurance"), "DEDUCTION", false),
                    Component("LEGACY_TOTAL_DEDUCTIONS", "Legacy Total Deductions", ReadDecimal(source, "TotalDeductions"), "DEDUCTION", false)
                };

                var target = new BsonDocument
                {
                    ["_id"] = sourceId,
                    ["CompanyId"] = companyId,
                    ["EmployeeId"] = employeeId,
                    ["UserId"] = ReadString(source, "UserId"),
                    ["Currency"] = ReadString(source, "Currency", "INR").ToUpperInvariant(),
                    ["PaymentFrequency"] = ReadString(source, "PaymentFrequency", "MONTHLY").ToUpperInvariant(),
                    ["AnnualCtc"] = ReadDecimal(source, "Ctc"),
                    ["MonthlyGross"] = ReadDecimal(source, "GrossSalary"),
                    ["EffectiveFrom"] = effectiveFrom,
                    ["EffectiveTo"] = effectiveTo.HasValue ? new BsonDateTime(effectiveTo.Value) : BsonNull.Value,
                    ["Status"] = active ? "ACTIVE" : "SUPERSEDED",
                    ["ApprovalStatus"] = active ? "APPROVED" : "SUPERSEDED",
                    ["Source"] = "LEGACY_MIGRATION",
                    ["Version"] = 1,
                    ["CreatedAtUtc"] = ReadDate(source, "CreatedAt", DateTime.UtcNow),
                    ["Components"] = components
                };
                await canonical.InsertOneAsync(target);
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static BsonDocument Component(string code, string name, decimal value, string kind, bool taxable) => new()
    {
        ["ComponentCode"] = code,
        ["ComponentName"] = name,
        ["Kind"] = kind,
        ["CalculationType"] = "FIXED",
        ["Value"] = value,
        ["IsTaxable"] = taxable,
        ["IsRecurring"] = true,
        ["IsStatutory"] = false,
        ["IsEmployerContribution"] = false
    };

    private static string ReadString(BsonDocument d, string name, string fallback = "") =>
        d.TryGetValue(name, out var value) && !value.IsBsonNull ? value.ToString() : fallback;

    private static decimal ReadDecimal(BsonDocument d, string name) =>
        d.TryGetValue(name, out var value) && !value.IsBsonNull ? value.ToDecimal() : 0m;

    private static bool ReadBoolean(BsonDocument d, string name, bool fallback) =>
        d.TryGetValue(name, out var value) && !value.IsBsonNull ? value.ToBoolean() : fallback;

    private static DateTime ReadDate(BsonDocument d, string name, DateTime fallback) =>
        d.TryGetValue(name, out var value) && value.IsBsonDateTime ? value.ToUniversalTime() : fallback;

    private static DateTime? ReadNullableDate(BsonDocument d, string name) =>
        d.TryGetValue(name, out var value) && !value.IsBsonNull && value.IsBsonDateTime ? value.ToUniversalTime().Date : null;
}