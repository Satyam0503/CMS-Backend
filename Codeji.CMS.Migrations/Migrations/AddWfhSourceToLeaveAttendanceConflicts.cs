using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Backfills the WFH source ID only from attendance linked by company, employee, and source-record ID.</summary>
public sealed class AddWfhSourceToLeaveAttendanceConflicts : IDataBackfillMigration
{
    public string Id => $"2026-10-26-{nameof(AddWfhSourceToLeaveAttendanceConflicts)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await BackfillFromAttendanceAsync(db, nameof(AttendanceModel), "AttendanceId");
        await BackfillFromAttendanceAsync(db, nameof(AttendanceDaySegment), "AttendanceDaySegmentId");
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task BackfillFromAttendanceAsync(IMongoDatabase db, string attendanceCollection, string attendanceIdField)
    {
        var conflicts = db.GetCollection<BsonDocument>(nameof(AttendancePayrollException));
        var pipeline = new[]
        {
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", attendanceCollection },
                { "localField", "ExistingAttendanceId" },
                { "foreignField", attendanceIdField },
                { "as", "linkedAttendance" }
            }),
            new BsonDocument("$match", new BsonDocument
            {
                { "Status", "PENDING_REVIEW" },
                { "ExceptionType", new BsonDocument("$in", new BsonArray { "LEAVE_ATTENDANCE_CONFLICT", "LEAVE_ATTENDANCE_SEGMENT_CONFLICT" }) },
                { "LeaveRequestId", new BsonDocument("$type", "string") },
                { "WorkFromHomeRequestId", new BsonDocument("$in", new BsonArray { BsonNull.Value, "" }) },
                { "linkedAttendance.0", new BsonDocument("$exists", true) },
                { "$expr", new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$linkedAttendance.SourceType", 0 }), "WFH_REQUEST" }),
                        new BsonDocument("$eq", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$linkedAttendance.CompanyId", 0 }), "$CompanyId" }),
                        new BsonDocument("$eq", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$linkedAttendance.UserId", 0 }), "$UserId" }),
                        new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", new BsonDocument("$arrayElemAt", new BsonArray { "$linkedAttendance.SourceId", 0 })), "string" }),
                        new BsonDocument("$ne", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$linkedAttendance.SourceId", 0 }), "" })
                    }) }
            }),
            new BsonDocument("$set", new BsonDocument("WorkFromHomeRequestId",
                new BsonDocument("$arrayElemAt", new BsonArray { "$linkedAttendance.SourceId", 0 }))),
            new BsonDocument("$unset", "linkedAttendance"),
            new BsonDocument("$merge", new BsonDocument
            {
                { "into", nameof(AttendancePayrollException) },
                { "on", "_id" },
                { "whenMatched", "merge" },
                { "whenNotMatched", "discard" }
            })
        };
        await conflicts.Aggregate<BsonDocument>(pipeline).ToListAsync();
    }
}
