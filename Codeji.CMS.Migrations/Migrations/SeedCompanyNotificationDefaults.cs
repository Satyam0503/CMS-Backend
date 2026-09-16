using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Utility.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class SeedCompanyNotificationDefaults : IMigration
{
    public string Id => $"2026-09-08-06-{nameof(SeedCompanyNotificationDefaults)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Codeji.CMS.Repository.Entities.Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var defaults = Enum.GetValues<EnumsHelper.NotificationPreferenceType>().ToDictionary(x => x, _ => true);
        var companies = db.GetCollection<BsonDocument>(nameof(Company));
        var filter = new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("IsDeleted", new BsonDocument("$ne", true)),
            new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("NotificationPreferences", new BsonDocument("$exists", false)),
                new BsonDocument("NotificationPreferences", new BsonDocument("$size", 0))
            })
        });
        var preferenceDocument = defaults.ToDictionary(x => x.Key.ToString(), x => (BsonValue)x.Value).ToBsonDocument();
        await companies.UpdateManyAsync(filter,
            new BsonDocumentUpdateDefinition<BsonDocument>(new BsonDocument("$set", new BsonDocument("NotificationPreferences", preferenceDocument))));
        await migrations.InsertOneAsync(new Codeji.CMS.Repository.Entities.Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
