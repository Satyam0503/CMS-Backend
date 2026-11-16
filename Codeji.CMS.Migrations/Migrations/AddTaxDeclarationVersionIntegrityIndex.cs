using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Protects the per-employee, per-tax-year declaration version sequence against concurrent submissions.
/// Stops before index creation when existing records need manual cleanup; it never rewrites declarations.
/// </summary>
public sealed class AddTaxDeclarationVersionIntegrityIndex : IMigration
{
    public string Id => $"2026-11-04-{nameof(AddTaxDeclarationVersionIntegrityIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var declarations = db.GetCollection<BsonDocument>("EmployeeTaxDeclaration");
        var invalidCount = await declarations.CountDocumentsAsync(new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("CompanyId", new BsonDocument("$exists", false)),
            new BsonDocument("CompanyId", new BsonDocument("$not", new BsonDocument("$type", "string"))),
            new BsonDocument("CompanyId", ""),
            new BsonDocument("EmployeeId", new BsonDocument("$exists", false)),
            new BsonDocument("EmployeeId", new BsonDocument("$not", new BsonDocument("$type", "string"))),
            new BsonDocument("EmployeeId", ""),
            new BsonDocument("FinancialYear", new BsonDocument("$exists", false)),
            new BsonDocument("FinancialYear", new BsonDocument("$not", new BsonDocument("$type", "string"))),
            new BsonDocument("FinancialYear", ""),
            new BsonDocument("Version", new BsonDocument("$not", new BsonDocument("$type", "int")))
        }));
        if (invalidCount > 0)
            throw new InvalidOperationException($"Cannot install tax-declaration version integrity index: {invalidCount} declaration(s) have missing or invalid tenant, employee, financial year, or version keys. Review those records before retrying; no records were changed.");

        var duplicatePipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "CompanyId", "$CompanyId" },
                        { "EmployeeId", "$EmployeeId" },
                        { "FinancialYear", "$FinancialYear" },
                        { "Version", "$Version" }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$match", new BsonDocument("count", new BsonDocument("$gt", 1))),
            new BsonDocument("$limit", 5)
        };
        var duplicates = await declarations.Aggregate<BsonDocument>(duplicatePipeline).ToListAsync();
        if (duplicates.Count > 0)
            throw new InvalidOperationException($"Cannot install tax-declaration version integrity index: duplicate company/employee/year/version groups exist ({string.Join("; ", duplicates.Select(x => x["_id"].ToJson()) )}). Review and reconcile those records before retrying; no records were changed.");

        await declarations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId")
                .Ascending("EmployeeId")
                .Ascending("FinancialYear")
                .Ascending("Version"),
            new CreateIndexOptions { Name = "ux_tax_declaration_company_employee_year_version", Unique = true }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
