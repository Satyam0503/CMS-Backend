using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddEmployeeIdSequenceIndexes : IMigration
{
    public string Id => $"2026-08-10-{nameof(AddEmployeeIdSequenceIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await db.GetCollection<BsonDocument>("EmpUser").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument { { "CompanyId", 1 }, { "EmployeeId", 1 } },
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_employee_company_employee_id",
                Unique = true,
                // MongoDB partial index filters don't support $ne (compiles to $not, which is rejected);
                // use $exists + $type + $gt instead to exclude missing/null/empty values.
                PartialFilterExpression = new BsonDocument
                {
                    {
                        "$and",
                        new BsonArray
                        {
                            new BsonDocument("EmployeeId", new BsonDocument("$exists", true)),
                            new BsonDocument("EmployeeId", new BsonDocument("$type", "string")),
                            new BsonDocument("EmployeeId", new BsonDocument("$gt", "")),
                        }
                    }
                }
            }));
        await db.GetCollection<BsonDocument>("EmployeeIdSequence").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument("CompanyId", 1), new CreateIndexOptions { Name = "ux_employee_id_sequence_company", Unique = true }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
