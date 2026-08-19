using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Attendance;

public interface IEffectiveOfficeScheduleService
{
    Task<CompanyOfficeSchedule?> ResolveAsync(string companyId, EmpUser employee, DateTime attendanceDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EffectiveOfficeScheduleDto>> ResolveManyAsync(string companyId, IEnumerable<string> userIds, DateTime attendanceDate, CancellationToken cancellationToken = default);
}

public sealed class EffectiveOfficeScheduleService(
    IMongoDbRepository<CompanyOfficeSchedule> schedules,
    IMongoDbRepository<EmployeeScheduleAssignment> employeeAssignments,
    IMongoDbRepository<DepartmentScheduleAssignment> departmentAssignments,
    IMongoDbRepository<EmpUser> employeeRepository) : IEffectiveOfficeScheduleService
{
    public async Task<CompanyOfficeSchedule?> ResolveAsync(string companyId, EmpUser employee, DateTime attendanceDate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var day = attendanceDate.Date;
        bool Effective(DateTime from, DateTime? to) => from.Date <= day && (!to.HasValue || to.Value.Date >= day);

        var employeeAssignment = (await employeeAssignments.GetAll(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.IsActive && !x.IsDeleted, withDefaultFilter: false))
            .Where(x => Effective(x.EffectiveFrom, x.EffectiveTo)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        var departmentAssignment = employeeAssignment is null && !string.IsNullOrWhiteSpace(employee.Department)
            ? (await departmentAssignments.GetAll(x => x.CompanyId == companyId && x.Department == employee.Department && x.IsActive && !x.IsDeleted, withDefaultFilter: false))
                .Where(x => Effective(x.EffectiveFrom, x.EffectiveTo)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault()
            : null;
        var scheduleId = employeeAssignment?.ScheduleId ?? departmentAssignment?.ScheduleId;
        var candidates = await schedules.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted, withDefaultFilter: false);
        return candidates.Where(x => Effective(x.EffectiveFrom, x.EffectiveTo))
            .Where(x => !string.IsNullOrEmpty(scheduleId) ? x.ScheduleId == scheduleId : x.IsDefault)
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
    }

    public async Task<IReadOnlyList<EffectiveOfficeScheduleDto>> ResolveManyAsync(string companyId, IEnumerable<string> userIds, DateTime attendanceDate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = userIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal).Take(1000).ToArray();
        if (ids.Length == 0) return [];

        var day = attendanceDate.Date;
        bool Effective(DateTime from, DateTime? to) => from.Date <= day && (!to.HasValue || to.Value.Date >= day);
        var employeeOverrides = await employeeAssignments.GetAll(x => x.CompanyId == companyId && ids.Contains(x.UserId) && x.IsActive && !x.IsDeleted, withDefaultFilter: false);
        var employeeByUser = employeeOverrides.Where(x => Effective(x.EffectiveFrom, x.EffectiveTo)).GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.EffectiveFrom).First(), StringComparer.Ordinal);

        // Resolve the requested users from the employee collection, rather than
        // accepting a client supplied department. This is the tenant-safe source
        // for department inheritance.
        var companyEmployees = await employeeRepository.GetAll(x => x.CompanyId == companyId && ids.Contains(x.UserId) && x.Status && !x.IsDeleted, withDefaultFilter: false);
        var departments = companyEmployees.Where(x => !employeeByUser.ContainsKey(x.UserId) && !string.IsNullOrWhiteSpace(x.Department))
            .Select(x => x.Department.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var departmentByName = departments.Length == 0
            ? new Dictionary<string, DepartmentScheduleAssignment>(StringComparer.OrdinalIgnoreCase)
            : (await departmentAssignments.GetAll(x => x.CompanyId == companyId && departments.Contains(x.Department) && x.IsActive && !x.IsDeleted, withDefaultFilter: false))
                .Where(x => Effective(x.EffectiveFrom, x.EffectiveTo)).GroupBy(x => x.Department, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.EffectiveFrom).First(), StringComparer.OrdinalIgnoreCase);
        var availableSchedules = (await schedules.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted, withDefaultFilter: false))
            .Where(x => Effective(x.EffectiveFrom, x.EffectiveTo)).ToList();
        var defaultSchedule = availableSchedules.Where(x => x.IsDefault).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        var schedulesById = availableSchedules.GroupBy(x => x.ScheduleId).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.EffectiveFrom).First(), StringComparer.Ordinal);

        return companyEmployees.Select(employee =>
        {
            var source = "CompanyDefault";
            string? scheduleId = null;
            if (employeeByUser.TryGetValue(employee.UserId, out var personal)) { scheduleId = personal.ScheduleId; source = "EmployeeOverride"; }
            else if (!string.IsNullOrWhiteSpace(employee.Department) && departmentByName.TryGetValue(employee.Department.Trim(), out var department)) { scheduleId = department.ScheduleId; source = "DepartmentAssignment"; }
            var schedule = !string.IsNullOrWhiteSpace(scheduleId) && schedulesById.TryGetValue(scheduleId, out var assigned) ? assigned : defaultSchedule;
            return new EffectiveOfficeScheduleDto
            {
                UserId = employee.UserId,
                ScheduleId = schedule?.ScheduleId,
                ScheduleName = schedule?.Name,
                StartTime = schedule?.StartTime.ToString(@"hh\:mm"),
                EndTime = schedule?.EndTime.ToString(@"hh\:mm"),
                PunchInAllowedFrom = schedule?.CheckInAllowedFrom?.ToString(@"hh\:mm"),
                PunchInAllowedUntil = schedule?.CheckInAllowedUntil?.ToString(@"hh\:mm"),
                PunchOutAllowedFrom = schedule?.CheckOutAllowedFrom?.ToString(@"hh\:mm"),
                PunchOutAllowedUntil = schedule?.CheckOutAllowedUntil?.ToString(@"hh\:mm"),
                Source = schedule is null ? "Unassigned" : source,
            };
        }).ToList();
    }
}
