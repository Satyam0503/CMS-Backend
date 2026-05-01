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

    public async Task<AttendanceModel> AddAsync(AttendanceModel attendance)
    {
        attendance.Date = attendance.Date.Date;
        await _collection.InsertOneAsync(attendance);
        return attendance;
    }

    public async Task<AttendanceModel?> GetByUserAndDateAsync(string userId, DateTime date)
    {
        var filter =
            Builders<AttendanceModel>.Filter.Eq(a => a.UserId, userId) &
            Builders<AttendanceModel>.Filter.Eq(a => a.Date, date.Date);

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AttendanceModel>> GetAllByUserAsync(string userId)
    {
        var filter = Builders<AttendanceModel>.Filter.Eq(a => a.UserId, userId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<AttendanceModel>> GetAllByDateRangeAsync(
        DateTime from,
        DateTime to,
        string[]? userIds = null)
    {
        var filter =
            Builders<AttendanceModel>.Filter.Gte(a => a.Date, from.Date) &
            Builders<AttendanceModel>.Filter.Lte(a => a.Date, to.Date);

        if (userIds != null && userIds.Any())
            filter &= Builders<AttendanceModel>.Filter.In(a => a.UserId, userIds);

        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<bool> UpdateAsync(string userId, DateTime date, AttendanceModel updatedModel)
    {
        updatedModel.Date = updatedModel.Date.Date;

        var filter =
            Builders<AttendanceModel>.Filter.Eq(a => a.UserId, userId) &
            Builders<AttendanceModel>.Filter.Eq(a => a.Date, date.Date);

        var result = await _collection.ReplaceOneAsync(filter, updatedModel);

        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public Task GetAll(Func<object, bool> value)
    {
        throw new NotImplementedException();
    }
}
