using Codeji.CMS.Repository.Entities;

public interface IAttendanceRepository
{
    Task<AttendanceModel> AddAsync(AttendanceModel attendance);

    Task<AttendanceModel?> GetByUserAndDateAsync(string companyId, string userId, DateTime date);

    Task<IEnumerable<AttendanceModel>> GetAllByUserAsync(string companyId, string userId);

    Task<IEnumerable<AttendanceModel>> GetAllByDateRangeAsync(
        string companyId,
        DateTime from,
        DateTime to,
        string[]? userIds = null
    );

    Task<bool> UpdateAsync(string companyId, string userId, DateTime date, AttendanceModel updatedModel);

    Task GetAll(Func<object, bool> value);
}
