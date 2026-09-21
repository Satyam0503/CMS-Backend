using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Recruitments;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class NormalizeRecruitmentTimezonesToIndia : IMigration
{
    public string Id => $"2026-09-21-{typeof(NormalizeRecruitmentTimezonesToIndia).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        await db.GetCollection<RecruitmentEnterpriseConfig>("RecruitmentEnterpriseConfig")
            .UpdateManyAsync(x => x.DefaultTimezone == "UTC" || string.IsNullOrWhiteSpace(x.DefaultTimezone), Builders<RecruitmentEnterpriseConfig>.Update.Set(x => x.DefaultTimezone, "Asia/Kolkata"));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
