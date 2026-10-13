using MongoDB.Driver;
using Codeji.CMS.Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly IMongoCollection<AttendanceModel> _collection;

    public AttendanceRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<AttendanceModel>("Attendance");
    }

    private static DateTime UtcMidnight(DateTime date) => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    private static DateTime UtcMidnight(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static FilterDefinition<AttendanceModel> BuildUserDateFilter(string companyId, string userId, DateTime date)
    {
        var dayStart = UtcMidnight(date);
        var dayEnd = dayStart.AddDays(1);
        return Builders<AttendanceModel>.Filter.Eq(a => a.CompanyId, companyId)
            & Builders<AttendanceModel>.Filter.Eq(a => a.UserId, userId)
            & Builders<AttendanceModel>.Filter.Gte(a => a.Date, dayStart)
            & Builders<AttendanceModel>.Filter.Lt(a => a.Date, dayEnd);
    }

    public async Task<AttendanceModel> AddAsync(AttendanceModel attendance)
    {
        attendance.Date = UtcMidnight(attendance.Date);
        if (string.IsNullOrWhiteSpace(attendance.AttendanceId)) attendance.AttendanceId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

        var dayFilter = BuildUserDateFilter(attendance.CompanyId, attendance.UserId ?? string.Empty, attendance.Date);
        var existingDocs = await _collection.Find(dayFilter).ToListAsync();

        if (existingDocs.Count > 0)
        {
            var keeper = existingDocs[0];
            if (!string.IsNullOrWhiteSpace(keeper.AttendanceId)) attendance.AttendanceId = keeper.AttendanceId;

            await _collection.ReplaceOneAsync(
                Builders<AttendanceModel>.Filter.Eq(a => a.AttendanceId, attendance.AttendanceId),
                attendance,
                new ReplaceOptions { IsUpsert = true });

            if (existingDocs.Count > 1)
            {
                var duplicateIds = existingDocs
                    .Where(x => x.AttendanceId != attendance.AttendanceId)
                    .Select(x => x.AttendanceId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (duplicateIds.Count > 0)
                {
                    await _collection.DeleteManyAsync(
                        Builders<AttendanceModel>.Filter.In(a => a.AttendanceId, duplicateIds));
                }
            }
        }
        else
        {
            await _collection.ReplaceOneAsync(
                dayFilter,
                attendance,
                new ReplaceOptions { IsUpsert = true });
        }

        return await _collection.Find(Builders<AttendanceModel>.Filter.Eq(a => a.AttendanceId, attendance.AttendanceId)).FirstAsync();
    }

    public async Task<AttendanceModel?> GetByUserAndDateAsync(string companyId, string userId, DateTime date)
    {
        var filter = BuildUserDateFilter(companyId, userId, date);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AttendanceModel>> GetAllByUserAsync(string companyId, string userId)
    {
        var filter = Builders<AttendanceModel>.Filter.Eq(a => a.CompanyId, companyId) &
                     Builders<AttendanceModel>.Filter.Eq(a => a.UserId, userId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<AttendanceModel>> GetAllByDateRangeAsync(
        string companyId,
        DateTime from,
        DateTime to,
        string[]? userIds = null)
    {
        var filter =
            Builders<AttendanceModel>.Filter.Eq(a => a.CompanyId, companyId) &
            Builders<AttendanceModel>.Filter.Gte(a => a.Date, UtcMidnight(from)) &
            Builders<AttendanceModel>.Filter.Lte(a => a.Date, UtcMidnight(to).AddDays(1).AddTicks(-1));

        if (userIds != null && userIds.Any())
            filter &= Builders<AttendanceModel>.Filter.In(a => a.UserId, userIds);

        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<bool> UpdateAsync(string companyId, string userId, DateTime date, AttendanceModel updatedModel)
    {
        updatedModel.Date = UtcMidnight(updatedModel.Date);

        // Existing historical rows may carry a time component even though attendance is
        // logically date-only. Reads already use this day-range identity; updates must do the
        // same so an HR/Admin correction does not fail after the record was successfully found.
        var filter = BuildUserDateFilter(companyId, userId, date);

        var result = await _collection.ReplaceOneAsync(filter, updatedModel);

        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public Task GetAll(Func<object, bool> value)
    {
        throw new NotImplementedException();
    }
}
