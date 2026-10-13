using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

public interface IOfficeScheduleSettingsService
{
    Task<Result<OfficeScheduleSettingsDto>> GetAsync(string companyId, CancellationToken cancellationToken = default);
    Task<Result<OfficeScheduleSettingsDto>> GetAllAsync(string companyId, CancellationToken cancellationToken = default);
    Task<Result<OfficeScheduleSettingsDto>> SaveAsync(string companyId, string actorUserId, OfficeScheduleSettingsDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string companyId, string actorUserId, string scheduleId, CancellationToken cancellationToken = default);
}

public sealed class OfficeScheduleSettingsService(
    IMongoDbRepository<CompanyOfficeSchedule> schedules,
    IMongoDbRepository<EmployeeScheduleAssignment> employeeAssignments,
    IMongoDbRepository<DepartmentScheduleAssignment> departmentAssignments) : IOfficeScheduleSettingsService
{
    public async Task<Result<OfficeScheduleSettingsDto>> GetAsync(string companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(companyId)) return Fail("OFFICE_SCHEDULE_COMPANY_REQUIRED");

        // Do not rely on the repository's ambient/default filter for tenant data.
        // A schedule must always be selected using the authenticated company key.
        var schedule = (await schedules.GetAll(
                x => x.CompanyId == companyId && x.IsActive && x.IsDefault && !x.IsDeleted,
                withDefaultFilter: false))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        return new Result<OfficeScheduleSettingsDto> { Success = true, MethodResult = schedule is null ? Default() : ToDto(schedule) };
    }

    public async Task<Result<OfficeScheduleSettingsDto>> GetAllAsync(string companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(companyId)) return Fail("OFFICE_SCHEDULE_COMPANY_REQUIRED");

        var entities = (await schedules.GetAll(x => x.CompanyId == companyId && !x.IsDeleted, withDefaultFilter: false)).ToList();
        // Legacy companies may only have the UI fallback rather than a persisted
        // schedule. Materialize it before adding another shift, otherwise the
        // first saved shift replaces that apparent default in the list.
        if (entities.Count == 0)
        {
            var defaultSchedule = CreateDefault(companyId);
            var createResult = await schedules.AddOne(defaultSchedule);
            if (!createResult.Success) return Fail("OFFICE_SCHEDULE_DEFAULT_CREATE_FAILED");
            entities.Add(defaultSchedule);
        }
        // Recover the short-lived UI-only-default release: it produced a single
        // persisted "Shift 2" that was incorrectly promoted to default because
        // the original default had never been stored. This exact legacy shape is
        // safe to repair without touching normally configured companies.
        else if (entities.Count == 1 && entities[0].IsDefault &&
                 string.Equals(entities[0].Name, "Shift 2", StringComparison.OrdinalIgnoreCase))
        {
            var defaultSchedule = CreateDefault(companyId);
            var createResult = await schedules.AddOne(defaultSchedule);
            if (!createResult.Success) return Fail("OFFICE_SCHEDULE_DEFAULT_CREATE_FAILED");
            var demoteResult = await schedules.UpdateMany(
                Builders<CompanyOfficeSchedule>.Filter.Where(x => x.CompanyId == companyId && x.ScheduleId == entities[0].ScheduleId),
                Builders<CompanyOfficeSchedule>.Update.Set(x => x.IsDefault, false).Set(x => x.UpdatedDate, DateTime.UtcNow));
            if (!demoteResult.Success) return Fail("OFFICE_SCHEDULE_DEFAULT_UPDATE_FAILED");
            entities[0].IsDefault = false;
            entities.Add(defaultSchedule);
        }
        var items = entities
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name).Select(ToDto).ToList();
        return new Result<OfficeScheduleSettingsDto> { Success = true, MethodResults = items };
    }

    public async Task<Result<OfficeScheduleSettingsDto>> SaveAsync(string companyId, string actorUserId, OfficeScheduleSettingsDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(companyId)) return Fail("OFFICE_SCHEDULE_COMPANY_REQUIRED");
        if (string.IsNullOrWhiteSpace(actorUserId)) return Fail("OFFICE_SCHEDULE_ACTOR_REQUIRED");

        request.Name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Name)) return Fail("OFFICE_SCHEDULE_NAME_REQUIRED");
        // Read this tenant's active schedules once and use it for both duplicate
        // checks. A shift is a distinct attendance window, not merely a name.
        var companySchedules = (await schedules.GetAll(
                x => x.CompanyId == companyId && !x.IsDeleted,
                withDefaultFilter: false))
            .ToList();
        var sameNameExists = companySchedules
            .Any(x => x.ScheduleId != request.ScheduleId && string.Equals(x.Name, request.Name, StringComparison.OrdinalIgnoreCase));
        if (sameNameExists) return Fail("OFFICE_SCHEDULE_NAME_EXISTS");
        if (!TryTime(request.StandardCheckIn, out var checkIn) || !TryTime(request.StandardCheckOut, out var checkOut) ||
            !TryTime(request.CheckInAllowedFrom, out var checkInFrom) || !TryTime(request.CheckInAllowedUntil, out var checkInUntil) ||
            !TryTime(request.CheckOutAllowedFrom, out var checkOutFrom) || !TryTime(request.CheckOutAllowedUntil, out var checkOutUntil))
            return Fail("OFFICE_SCHEDULE_TIME_INVALID");
        if (request.GraceMinutes < 0 || request.GraceMinutes > 180 || checkInFrom > checkInUntil || checkOutFrom > checkOutUntil || checkIn < checkInFrom || checkIn > checkInUntil || checkOut < checkOutFrom || checkOut > checkOutUntil)
            return Fail("OFFICE_SCHEDULE_WINDOW_INVALID");
        var scheduledMinutes = (int)(checkOut - checkIn).TotalMinutes - request.BreakMinutes;
        if (scheduledMinutes < request.RequiredWorkingMinutes)
            return Fail("OFFICE_SCHEDULE_MINIMUM_HOURS_INVALID");
        if (companySchedules.Any(x => x.ScheduleId != request.ScheduleId && x.StartTime == checkIn && x.EndTime == checkOut))
            return Fail("OFFICE_SCHEDULE_TIMING_EXISTS");

        // GetAll with the explicit tenant predicate avoids depending on implicit
        // repository filtering and prevents a supplied ScheduleId from ever
        // resolving a schedule owned by a different company.
        var existing = !string.IsNullOrWhiteSpace(request.ScheduleId)
            ? (await schedules.GetAll(
                    x => x.CompanyId == companyId && x.ScheduleId == request.ScheduleId && !x.IsDeleted,
                    withDefaultFilter: false)).FirstOrDefault()
            : null;
        if (!string.IsNullOrWhiteSpace(request.ScheduleId) && existing is null)
            return Fail("OFFICE_SCHEDULE_NOT_FOUND");
        var defaultSchedule = (await schedules.GetAll(x => x.CompanyId == companyId && x.IsDefault && !x.IsDeleted, withDefaultFilter: false)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        if (existing is null && request.IsDefault && defaultSchedule is not null)
            await schedules.UpdateMany(Builders<CompanyOfficeSchedule>.Filter.Where(x => x.CompanyId == companyId && x.IsDefault && !x.IsDeleted), Builders<CompanyOfficeSchedule>.Update.Set(x => x.IsDefault, false));
        if (existing is not null && request.IsDefault && !existing.IsDefault)
            await schedules.UpdateMany(Builders<CompanyOfficeSchedule>.Filter.Where(x => x.CompanyId == companyId && x.IsDefault && x.ScheduleId != existing.ScheduleId && !x.IsDeleted), Builders<CompanyOfficeSchedule>.Update.Set(x => x.IsDefault, false));
        var entity = existing ?? new CompanyOfficeSchedule { CompanyId = companyId, ScheduleId = Guid.NewGuid().ToString(), CreatedBy = actorUserId, CreatedDate = DateTime.UtcNow };
        // CompanyId is server-owned. Never accept a tenant value from the request
        // or allow an existing entity to be written under another company.
        entity.CompanyId = companyId;
        entity.Name = request.Name.Trim(); entity.StartTime = checkIn; entity.EndTime = checkOut;
        entity.CheckInAllowedFrom = checkInFrom; entity.CheckInAllowedUntil = checkInUntil; entity.CheckOutAllowedFrom = checkOutFrom; entity.CheckOutAllowedUntil = checkOutUntil;
        entity.GraceMinutes = request.GraceMinutes; entity.BreakMinutes = request.BreakMinutes; entity.RequiredWorkingMinutes = request.RequiredWorkingMinutes;
        entity.IsActive = request.IsActive; entity.IsDefault = request.IsDefault || existing?.IsDefault == true || (defaultSchedule is null && existing is null); entity.EffectiveFrom = DateTime.UtcNow.Date; entity.EffectiveTo = null; entity.TimeZoneId = "Asia/Kolkata"; entity.UpdatedBy = actorUserId; entity.UpdatedDate = DateTime.UtcNow; entity.Version++;
        var writeResult = existing is null
            ? await schedules.AddOne(entity)
            : await schedules.Update(Builders<CompanyOfficeSchedule>.Filter.And(
                    Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.CompanyId, companyId),
                    Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.ScheduleId, entity.ScheduleId),
                    Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.IsDeleted, false)), entity);
        if (!writeResult.Success)
        {
            return Fail("OFFICE_SCHEDULE_SAVE_FAILED");
        }
        return new Result<OfficeScheduleSettingsDto> { Success = true, MethodResult = ToDto(entity), Message = "Office schedule saved." };
    }

    public async Task<Result> DeleteAsync(string companyId, string actorUserId, string scheduleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(companyId)) return Failure("OFFICE_SCHEDULE_COMPANY_REQUIRED");
        if (string.IsNullOrWhiteSpace(actorUserId)) return Failure("OFFICE_SCHEDULE_ACTOR_REQUIRED");
        if (string.IsNullOrWhiteSpace(scheduleId)) return Failure("OFFICE_SCHEDULE_ID_REQUIRED");

        // ScheduleId is never sufficient on its own. The lookup and update are
        // both constrained by the authenticated company so another tenant's
        // shift cannot be read, deleted, or inferred from this endpoint.
        var schedule = (await schedules.GetAll(
                x => x.CompanyId == companyId && x.ScheduleId == scheduleId && !x.IsDeleted,
                withDefaultFilter: false))
            .FirstOrDefault();
        if (schedule is null) return Failure("OFFICE_SCHEDULE_NOT_FOUND");
        if (schedule.IsDefault) return Failure("OFFICE_SCHEDULE_DEFAULT_CANNOT_BE_DELETED");

        // Removing an in-use shift would change the attendance behaviour of
        // assigned employees or departments. Keep its historical record and
        // require the assignment to be moved before the admin deletes it.
        var hasEmployeeAssignments = (await employeeAssignments.GetAll(
                x => x.CompanyId == companyId && x.ScheduleId == scheduleId && x.IsActive && !x.IsDeleted,
                withDefaultFilter: false))
            .Any();
        var hasDepartmentAssignments = (await departmentAssignments.GetAll(
                x => x.CompanyId == companyId && x.ScheduleId == scheduleId && x.IsActive && !x.IsDeleted,
                withDefaultFilter: false))
            .Any();
        if (hasEmployeeAssignments || hasDepartmentAssignments)
            return Failure("OFFICE_SCHEDULE_IN_USE");

        var now = DateTime.UtcNow;
        var result = await schedules.UpdateMany(
            Builders<CompanyOfficeSchedule>.Filter.And(
                Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.CompanyId, companyId),
                Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.ScheduleId, scheduleId),
                Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.IsDeleted, false),
                Builders<CompanyOfficeSchedule>.Filter.Eq(x => x.IsDefault, false)),
            Builders<CompanyOfficeSchedule>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.IsActive, false)
                .Set(x => x.UpdatedBy, actorUserId)
                .Set(x => x.UpdatedDate, now));
        return result.Success
            ? new Result { Success = true, Message = "Office schedule deleted." }
            : Failure("OFFICE_SCHEDULE_DELETE_FAILED");
    }

    private static bool TryTime(string value, out TimeSpan time) => TimeSpan.TryParse(value, out time);
    private static Result<OfficeScheduleSettingsDto> Fail(string message) => new() { Success = false, Message = message };
    private static Result Failure(string message) => new() { Success = false, Message = message };
    private static OfficeScheduleSettingsDto Default() => new();
    private static CompanyOfficeSchedule CreateDefault(string companyId) => new()
    {
        CompanyId = companyId, ScheduleId = Guid.NewGuid().ToString(), Name = "Default shift",
        StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(18),
        CheckInAllowedFrom = TimeSpan.FromHours(8.5), CheckInAllowedUntil = TimeSpan.FromHours(10),
        CheckOutAllowedFrom = TimeSpan.FromHours(17.5), CheckOutAllowedUntil = TimeSpan.FromHours(19),
        BreakMinutes = 60, RequiredWorkingMinutes = 480, EffectiveFrom = DateTime.UtcNow.Date,
        IsActive = true, IsDefault = true, CreatedBy = "system", CreatedDate = DateTime.UtcNow
    };
    private static OfficeScheduleSettingsDto ToDto(CompanyOfficeSchedule value) => new()
    {
        ScheduleId = value.ScheduleId, Name = value.Name, IsDefault = value.IsDefault, IsActive = value.IsActive, StandardCheckIn = value.StartTime.ToString(@"hh\:mm"), StandardCheckOut = value.EndTime.ToString(@"hh\:mm"),
        CheckInAllowedFrom = (value.CheckInAllowedFrom ?? TimeSpan.FromHours(8.5)).ToString(@"hh\:mm"), CheckInAllowedUntil = (value.CheckInAllowedUntil ?? TimeSpan.FromHours(10)).ToString(@"hh\:mm"),
        CheckOutAllowedFrom = (value.CheckOutAllowedFrom ?? TimeSpan.FromHours(17.5)).ToString(@"hh\:mm"), CheckOutAllowedUntil = (value.CheckOutAllowedUntil ?? TimeSpan.FromHours(19)).ToString(@"hh\:mm"),
        GraceMinutes = Math.Max(value.GraceMinutes, 0), BreakMinutes = value.BreakMinutes, RequiredWorkingMinutes = value.RequiredWorkingMinutes <= 0 ? 480 : value.RequiredWorkingMinutes,
    };
}
