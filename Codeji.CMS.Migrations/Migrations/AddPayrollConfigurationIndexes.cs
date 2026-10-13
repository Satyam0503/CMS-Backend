using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddPayrollConfigurationIndexes : IMigration
{
    public string Id => $"2026-09-28-{nameof(AddPayrollConfigurationIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        await EnsureUniqueKeyIsClean(db, "PayrollPayGroup", "CompanyId", "Name");
        await EnsureUniqueKeyIsClean(db, "PayrollPolicyProfile", "CompanyId", "Name", "EffectiveFrom");
        await EnsureUniqueKeyIsClean(db, "SalaryTemplate", "CompanyId", "Name", "Version");
        await EnsureUniqueKeyIsClean(db, "PayrollSalaryComponentDefinition", "CompanyId", "Code", "Version");
        await Create(db, "PayrollPayGroup", "ux_paygroup_company_name", true, "CompanyId", "Name");
        await Create(db, "PayrollPayGroup", "ix_paygroup_company_effective", false, "CompanyId", "EffectiveFrom", "EffectiveTo");
        await Create(db, "PayrollPolicyProfile", "ux_policy_profile_company_name_start", true, "CompanyId", "Name", "EffectiveFrom");
        await Create(db, "PayrollPolicyProfile", "ix_policy_profile_company_effective", false, "CompanyId", "EffectiveFrom", "EffectiveTo");
        await Create(db, "SalaryTemplate", "ux_salary_template_company_name_version", true, "CompanyId", "Name", "Version");
        await Create(db, "SalaryTemplate", "ix_salary_template_company_effective", false, "CompanyId", "EffectiveFrom", "EffectiveTo");
        await Create(db, "PayrollSalaryComponentDefinition", "ux_salary_component_company_code_version", true, "CompanyId", "Code", "Version");
        await Create(db, "PayrollSalaryComponentDefinition", "ix_salary_component_company_effective", false, "CompanyId", "EffectiveFrom", "EffectiveTo");
        await Create(db, "EmployeePayrollAssignment", "ix_payroll_assignment_company_employee_effective", false, "CompanyId", "EmployeeId", "EffectiveFrom", "EffectiveTo");
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static Task Create(IMongoDatabase db, string collection, string name, bool unique, params string[] fields) =>
        db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x => new BsonElement(x, 1))), new CreateIndexOptions { Name = name, Unique = unique }));

    private static async Task EnsureUniqueKeyIsClean(IMongoDatabase db, string collectionName, params string[] fields)
    {
        var key = new BsonDocument(fields.Select(field => new BsonElement(field, "$" + field)));
        var duplicate = await db.GetCollection<BsonDocument>(collectionName).Aggregate()
            .Group(new BsonDocument { { "_id", key }, { "count", new BsonDocument("$sum", 1) } })
            .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
            .FirstOrDefaultAsync();
        if (duplicate is not null)
            throw new InvalidOperationException($"{collectionName} contains duplicate rows for unique key ({string.Join(", ", fields)}). Resolve duplicates before applying the payroll configuration indexes.");
    }
}
