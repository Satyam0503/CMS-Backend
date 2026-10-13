using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Backfills only absent presentation/leave-selection fields on existing attendance statuses.</summary>
public sealed class BackfillAttendanceStatusPresentationAndLeaveEligibility : IMigration
{
    public string Id => $"2026-08-01-{nameof(BackfillAttendanceStatusPresentationAndLeaveEligibility)}";
    private static readonly Dictionary<string, (string Color, bool LeaveEligible)> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["P"] = ("#2E7D32", false), ["A"] = ("#D32F2F", false), ["SL"] = ("#7B1FA2", true), ["CL"] = ("#1565C0", true),
        ["EL"] = ("#00838F", true), ["WFH"] = ("#0277BD", false), ["HD"] = ("#EF6C00", false), ["ED"] = ("#6D4C41", false),
        ["LHD"] = ("#F57C00", false), ["WFH+WFO"] = ("#00838F", false), ["COMP-OFF"] = ("#388E3C", true),
        ["CL-HALF"] = ("#5C6BC0", true), ["SL-HALF"] = ("#8E24AA", true), ["WFH-HD"] = ("#039BE5", false), ["UL"] = ("#C62828", true)
    };

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var statuses = db.GetCollection<BsonDocument>("AttendanceStatusSetting");
        foreach (var item in await statuses.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
        {
            var code = item.GetValue("Code", string.Empty).AsString;
            var defaults = Defaults.TryGetValue(code, out var known) ? known : ("#607D8B", false);
            var updates = Builders<BsonDocument>.Update;
            var changes = new List<UpdateDefinition<BsonDocument>>();
            if (!item.Contains("ColorHex") || string.IsNullOrWhiteSpace(item.GetValue("ColorHex", string.Empty).AsString)) changes.Add(updates.Set("ColorHex", defaults.Item1));
            if (!item.Contains("IsAvailableForLeaveManagement")) changes.Add(updates.Set("IsAvailableForLeaveManagement", defaults.Item2));
            if (changes.Count > 0) await statuses.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", item["_id"]), updates.Combine(changes));
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
