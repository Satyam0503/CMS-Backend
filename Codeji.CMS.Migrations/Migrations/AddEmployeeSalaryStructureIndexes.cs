using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddEmployeeSalaryStructureIndexes : IMigration
{
    public string Id => $"2026-09-30-{nameof(AddEmployeeSalaryStructureIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var collection = db.GetCollection<BsonDocument>("EmployeeSalaryStructure");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(new[] { new BsonElement("CompanyId", 1), new BsonElement("EmployeeId", 1), new BsonElement("EffectiveFrom", 1), new BsonElement("EffectiveTo", 1) }), new CreateIndexOptions { Name = "ix_employee_salary_company_employee_effective" }));
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(new[] { new BsonElement("CompanyId", 1), new BsonElement("EmployeeId", 1), new BsonElement("ApprovalStatus", 1), new BsonElement("Status", 1) }), new CreateIndexOptions { Name = "ix_employee_salary_company_employee_status" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
