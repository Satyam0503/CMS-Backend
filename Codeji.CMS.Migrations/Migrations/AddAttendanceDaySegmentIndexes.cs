using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Creates the additive half-day source ownership collection and indexes.</summary>
public sealed class AddAttendanceDaySegmentIndexes : IMigration
{
    public string Id => $"2026-08-04-{nameof(AddAttendanceDaySegmentIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var collection = db.GetCollection<BsonDocument>("AttendanceDaySegment");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument { { "CompanyId", 1 }, { "UserId", 1 }, { "Date", 1 }, { "Segment", 1 } },
            new CreateIndexOptions { Name = "ux_attendance_segment_company_user_date_half", Unique = true }));
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument { { "CompanyId", 1 }, { "SourceType", 1 }, { "SourceId", 1 } },
            new CreateIndexOptions { Name = "ix_attendance_segment_source" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
