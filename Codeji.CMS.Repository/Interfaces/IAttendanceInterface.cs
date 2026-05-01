using Codeji.CMS.Repository.Entities;

public interface IAttendanceRepository
{
    Task<AttendanceModel> AddAsync(AttendanceModel attendance);

    Task<AttendanceModel?> GetByUserAndDateAsync(string userId, DateTime date);

    Task<IEnumerable<AttendanceModel>> GetAllByUserAsync(string userId);

    Task<IEnumerable<AttendanceModel>> GetAllByDateRangeAsync(
        DateTime from,
        DateTime to,
        string[]? userIds = null
    );

    Task<bool> UpdateAsync(string userId, DateTime date, AttendanceModel updatedModel);

    Task GetAll(Func<object, bool> value);
}
