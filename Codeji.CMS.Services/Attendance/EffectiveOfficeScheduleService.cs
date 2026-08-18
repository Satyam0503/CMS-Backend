using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Attendance;

public interface IEffectiveOfficeScheduleService
{
    Task<CompanyOfficeSchedule?> ResolveAsync(string companyId, EmpUser employee, DateTime attendanceDate, CancellationToken cancellationToken = default);
}

public sealed class EffectiveOfficeScheduleService(
    IMongoDbRepository<CompanyOfficeSchedule> schedules,
    IMongoDbRepository<EmployeeScheduleAssignment> employeeAssignments,
    IMongoDbRepository<DepartmentScheduleAssignment> departmentAssignments) : IEffectiveOfficeScheduleService
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
}
