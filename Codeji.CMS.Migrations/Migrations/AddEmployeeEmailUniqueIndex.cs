using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddEmployeeEmailUniqueIndex : IMigration
{
    public string Id => $"2026-08-11-{nameof(AddEmployeeEmailUniqueIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await db.GetCollection<BsonDocument>("EmpUser").Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("Email", 1),
                new CreateIndexOptions
                {
                    Name = "ux_employee_email_ci",
                    Unique = true,
                    Collation = new Collation("en", strength: CollationStrength.Secondary)
                }));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
