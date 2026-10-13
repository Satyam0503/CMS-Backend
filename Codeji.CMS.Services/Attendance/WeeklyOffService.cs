using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using MongoDB.Driver;

public interface IWeeklyOffService
{
    Task<WeeklyOffSettingDto> Get(string companyId);
    Task<Result> Save(string companyId, string userId, WeeklyOffSettingDto dto);
}

public class WeeklyOffService(
    IMongoDbRepository<WeeklyOffSetting> repository,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    IMongoDbRepository<EmpPayRoll> payrolls,
    IAttendanceInitializationService initialization) : IWeeklyOffService
{
    public async Task<WeeklyOffSettingDto> Get(string companyId)
    {
        var setting = await repository.FirstOrDefault(x => x.CompanyId == companyId);
        return new WeeklyOffSettingDto { OffDays = setting?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday] };
    }

    public async Task<Result> Save(string companyId, string userId, WeeklyOffSettingDto dto)
    {
        var days = dto.OffDays.Distinct().OrderBy(x => x).ToList();
        if (days.Count == 0 || days.Any(x => x < 0 || x > 6)) return new Result { Success = false, Message = "Select at least one valid weekday." };
        var setting = await repository.FirstOrDefault(x => x.CompanyId == companyId);
        var previousDays = setting?.OffDays?.Distinct().ToHashSet() ?? new HashSet<int> { (int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday };
        Result saved;
        if (setting == null)
            saved = await repository.AddOne(new WeeklyOffSetting { CompanyId = companyId, OffDays = days, CreatedBy = userId });
        else
        {
            setting.OffDays = days; setting.UpdatedBy = userId; setting.UpdatedDate = DateTime.UtcNow;
            saved = await repository.Update(Builders<WeeklyOffSetting>.Filter.Eq(x => x.Id, setting.Id), setting);
        }
        if (!saved.Success) return saved;

        // A weekly-off change must not leave a system-created Present record counting as
        // attendance. Manual, Leave and WFH records remain untouched. Locked/payrolled
        // periods are final and are deliberately not rewritten.
        var automaticRows = await attendance.GetAll(x => x.CompanyId == companyId &&
            (x.SourceType == AttendanceSourceTransitionPolicy.DefaultPresent || x.SourceType == "SYSTEM_OFFICE_START_PRESENT"));
        var invalidatedSummaries = new HashSet<(string UserId, DateTime Month)>();
        foreach (var row in automaticRows.Where(x => days.Contains((int)x.Date.DayOfWeek)))
        {
            var month = new DateTime(row.Date.Year, row.Date.Month, 1);
            var isLocked = await summaries.Exist(x => x.CompanyId == companyId && x.UserId == row.UserId && x.PayrollMonth == month && x.IsLocked);
            var isPayrolled = await payrolls.Exist(x => x.CompanyId == companyId && x.UserId == row.UserId && x.PayMonth == month && x.IsProcessed);
            if (isLocked || isPayrolled) continue;

            var removed = await attendance.Delete(Builders<AttendanceModel>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, row.AttendanceId) &
                Builders<AttendanceModel>.Filter.In(x => x.SourceType, new[] { AttendanceSourceTransitionPolicy.DefaultPresent, "SYSTEM_OFFICE_START_PRESENT" }));
            if (!removed.Success) return removed;
            if (!string.IsNullOrWhiteSpace(row.UserId)) invalidatedSummaries.Add((row.UserId, month));
        }
        foreach (var (affectedUserId, month) in invalidatedSummaries)
            await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
                x.CompanyId == companyId && x.UserId == affectedUserId && x.PayrollMonth == month && !x.IsLocked));

        // When an off day becomes a working day, populate the missing automatic Present
        // rows for the current month only. Historical periods remain governed by their
        // existing lock/payroll controls.
        if (previousDays.Except(days).Any())
            await initialization.InitializeMonthAsync(companyId, userId, DateTime.UtcNow, CancellationToken.None);

        return saved;
    }
}
