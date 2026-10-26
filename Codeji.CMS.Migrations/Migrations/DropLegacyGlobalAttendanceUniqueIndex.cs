using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Removes the retired cross-tenant attendance index without modifying attendance rows.</summary>
public sealed class DropLegacyGlobalAttendanceUniqueIndex : IMigration
{
    public string Id => $"2026-10-26-{nameof(DropLegacyGlobalAttendanceUniqueIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var attendance = db.GetCollection<BsonDocument>("Attendance");
        try
        {
            await attendance.Indexes.DropOneAsync("ux_attendance_user_date");
        }
        catch (MongoCommandException ex) when (ex.Code == 27 || ex.CodeName == "IndexNotFound")
        {
            // The retired global index is already absent.
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
