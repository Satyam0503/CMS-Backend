using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

public interface IOfficeScheduleSettingsService
{
    Task<Result<OfficeScheduleSettingsDto>> GetAsync(string companyId, CancellationToken cancellationToken = default);
    Task<Result<OfficeScheduleSettingsDto>> SaveAsync(string companyId, string actorUserId, OfficeScheduleSettingsDto request, CancellationToken cancellationToken = default);
}

public sealed class OfficeScheduleSettingsService(IMongoDbRepository<CompanyOfficeSchedule> schedules) : IOfficeScheduleSettingsService
{
    public async Task<Result<OfficeScheduleSettingsDto>> GetAsync(string companyId, CancellationToken cancellationToken = default)
    {
        var schedule = (await schedules.GetAll(x => x.CompanyId == companyId && x.IsActive && x.IsDefault && !x.IsDeleted))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        return new Result<OfficeScheduleSettingsDto> { Success = true, MethodResult = schedule is null ? Default() : ToDto(schedule) };
    }

    public async Task<Result<OfficeScheduleSettingsDto>> SaveAsync(string companyId, string actorUserId, OfficeScheduleSettingsDto request, CancellationToken cancellationToken = default)
    {
        if (!TryTime(request.StandardCheckIn, out var checkIn) || !TryTime(request.StandardCheckOut, out var checkOut) ||
            !TryTime(request.CheckInAllowedFrom, out var checkInFrom) || !TryTime(request.CheckInAllowedUntil, out var checkInUntil) ||
            !TryTime(request.CheckOutAllowedFrom, out var checkOutFrom) || !TryTime(request.CheckOutAllowedUntil, out var checkOutUntil))
            return Fail("OFFICE_SCHEDULE_TIME_INVALID");
        if (checkInFrom > checkInUntil || checkOutFrom > checkOutUntil || checkIn < checkInFrom || checkIn > checkInUntil || checkOut < checkOutFrom || checkOut > checkOutUntil)
            return Fail("OFFICE_SCHEDULE_WINDOW_INVALID");
        var scheduledMinutes = (int)(checkOut - checkIn).TotalMinutes - request.BreakMinutes;
        if (scheduledMinutes < request.RequiredWorkingMinutes)
            return Fail("OFFICE_SCHEDULE_MINIMUM_HOURS_INVALID");

        var existing = (await schedules.GetAll(x => x.CompanyId == companyId && x.IsDefault && !x.IsDeleted)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        var entity = existing ?? new CompanyOfficeSchedule { CompanyId = companyId, ScheduleId = Guid.NewGuid().ToString(), CreatedBy = actorUserId, CreatedDate = DateTime.UtcNow };
        entity.Name = request.Name.Trim(); entity.StartTime = checkIn; entity.EndTime = checkOut;
        entity.CheckInAllowedFrom = checkInFrom; entity.CheckInAllowedUntil = checkInUntil; entity.CheckOutAllowedFrom = checkOutFrom; entity.CheckOutAllowedUntil = checkOutUntil;
        entity.BreakMinutes = request.BreakMinutes; entity.RequiredWorkingMinutes = request.RequiredWorkingMinutes;
        entity.IsActive = true; entity.IsDefault = true; entity.EffectiveFrom = DateTime.UtcNow.Date; entity.EffectiveTo = null; entity.TimeZoneId = "Asia/Kolkata"; entity.UpdatedBy = actorUserId; entity.UpdatedDate = DateTime.UtcNow; entity.Version++;
        if (existing is null) await schedules.AddOne(entity); else await schedules.Update(Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.ScheduleId, entity.ScheduleId), entity);
        return new Result<OfficeScheduleSettingsDto> { Success = true, MethodResult = ToDto(entity), Message = "Office schedule saved." };
    }

    private static bool TryTime(string value, out TimeSpan time) => TimeSpan.TryParse(value, out time);
    private static Result<OfficeScheduleSettingsDto> Fail(string message) => new() { Success = false, Message = message };
    private static OfficeScheduleSettingsDto Default() => new();
    private static OfficeScheduleSettingsDto ToDto(CompanyOfficeSchedule value) => new()
    {
        ScheduleId = value.ScheduleId, Name = value.Name, StandardCheckIn = value.StartTime.ToString(@"hh\:mm"), StandardCheckOut = value.EndTime.ToString(@"hh\:mm"),
        CheckInAllowedFrom = (value.CheckInAllowedFrom ?? TimeSpan.FromHours(8.5)).ToString(@"hh\:mm"), CheckInAllowedUntil = (value.CheckInAllowedUntil ?? TimeSpan.FromHours(10)).ToString(@"hh\:mm"),
        CheckOutAllowedFrom = (value.CheckOutAllowedFrom ?? TimeSpan.FromHours(17.5)).ToString(@"hh\:mm"), CheckOutAllowedUntil = (value.CheckOutAllowedUntil ?? TimeSpan.FromHours(19)).ToString(@"hh\:mm"),
        BreakMinutes = value.BreakMinutes, RequiredWorkingMinutes = value.RequiredWorkingMinutes <= 0 ? 480 : value.RequiredWorkingMinutes,
    };
}
