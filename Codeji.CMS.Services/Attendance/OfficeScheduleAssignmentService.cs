using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

public interface IOfficeScheduleAssignmentService
{
    Task<Result<EmployeeOfficeScheduleAssignmentDto>> GetEmployeeAsync(string companyId, string userId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeOfficeScheduleAssignmentDto>> SetEmployeeAsync(string companyId, string actorUserId, string userId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<DepartmentOfficeScheduleAssignmentDto>> GetDepartmentsAsync(string companyId, CancellationToken cancellationToken = default);
    Task<Result<DepartmentOfficeScheduleAssignmentDto>> SetDepartmentAsync(string companyId, string actorUserId, string departmentId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Maintains company-scoped shift mappings. Employee mappings override department mappings.</summary>
public sealed class OfficeScheduleAssignmentService(
    IMongoDbRepository<CompanyOfficeSchedule> schedules,
    IMongoDbRepository<EmployeeScheduleAssignment> employeeAssignments,
    IMongoDbRepository<DepartmentScheduleAssignment> departmentAssignments,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<Department> departments) : IOfficeScheduleAssignmentService
{
    public async Task<Result<EmployeeOfficeScheduleAssignmentDto>> GetEmployeeAsync(string companyId, string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var employee = await Employee(companyId, userId);
        if (employee is null) return Fail<EmployeeOfficeScheduleAssignmentDto>("EMPLOYEE_NOT_FOUND");

        var assignment = (await employeeAssignments.GetAll(x => x.CompanyId == companyId && x.UserId == userId && x.IsActive && !x.IsDeleted, withDefaultFilter: false))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        var schedule = assignment is null ? null : await Schedule(companyId, assignment.ScheduleId);
        return new Result<EmployeeOfficeScheduleAssignmentDto>
        {
            Success = true,
            MethodResult = new EmployeeOfficeScheduleAssignmentDto
            {
                UserId = userId,
                ScheduleId = schedule?.ScheduleId,
                ScheduleName = schedule?.Name,
                Source = assignment is null ? "DepartmentOrCompanyDefault" : "EmployeeOverride"
            }
        };
    }

    public async Task<Result<EmployeeOfficeScheduleAssignmentDto>> SetEmployeeAsync(string companyId, string actorUserId, string userId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(actorUserId)) return Fail<EmployeeOfficeScheduleAssignmentDto>("OFFICE_SCHEDULE_ACTOR_REQUIRED");
        if (await Employee(companyId, userId) is null) return Fail<EmployeeOfficeScheduleAssignmentDto>("EMPLOYEE_NOT_FOUND");
        var scheduleId = request.ScheduleId?.Trim();
        var schedule = await ValidateSchedule(companyId, scheduleId);
        if (!schedule.Success) return Fail<EmployeeOfficeScheduleAssignmentDto>(schedule.Message);

        var existing = (await employeeAssignments.GetAll(x => x.CompanyId == companyId && x.UserId == userId && x.IsActive && !x.IsDeleted, withDefaultFilter: false)).ToList();
        var current = existing.OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();

        // A repeated save must not create another active assignment. It also avoids
        // a needless close-and-reinsert cycle for an unchanged employee override.
        if (current is not null && string.Equals(current.ScheduleId, scheduleId, StringComparison.Ordinal))
            return await GetEmployeeAsync(companyId, userId, cancellationToken);

        var now = DateTime.UtcNow;
        // There is nothing to close for an employee currently inheriting the
        // department/company shift. UpdateMany reports zero modifications as a
        // failure, so call it only when an active company-scoped mapping exists.
        if (existing.Count > 0)
        {
            var close = await employeeAssignments.UpdateMany(
                Builders<EmployeeScheduleAssignment>.Filter.Where(x => x.CompanyId == companyId && x.UserId == userId && x.IsActive && !x.IsDeleted),
                Builders<EmployeeScheduleAssignment>.Update.Set(x => x.IsActive, false).Set(x => x.EffectiveTo, now.Date.AddDays(-1)).Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, now));
            if (!close.Success) return Fail<EmployeeOfficeScheduleAssignmentDto>("OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED");
        }

        if (!string.IsNullOrWhiteSpace(scheduleId))
        {
            var insert = await employeeAssignments.AddOne(new EmployeeScheduleAssignment
            {
                AssignmentId = Guid.NewGuid().ToString(), CompanyId = companyId, UserId = userId, ScheduleId = scheduleId,
                EffectiveFrom = now.Date, IsActive = true, CreatedBy = actorUserId, CreatedDate = now
            });
            if (!insert.Success) return Fail<EmployeeOfficeScheduleAssignmentDto>("OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED");
        }

        return await GetEmployeeAsync(companyId, userId, cancellationToken);
    }

    public async Task<Result<DepartmentOfficeScheduleAssignmentDto>> GetDepartmentsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var active = await departmentAssignments.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted, withDefaultFilter: false);
        var companySchedules = await schedules.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted, withDefaultFilter: false);
        var names = companySchedules.ToDictionary(x => x.ScheduleId, x => x.Name);
        return new Result<DepartmentOfficeScheduleAssignmentDto>
        {
            Success = true,
            MethodResults = active.OrderByDescending(x => x.EffectiveFrom).GroupBy(x => x.Department).Select(x => x.First())
                .Select(x => new DepartmentOfficeScheduleAssignmentDto { DepartmentId = x.Department, ScheduleId = x.ScheduleId, ScheduleName = names.GetValueOrDefault(x.ScheduleId) }).ToList()
        };
    }

    public async Task<Result<DepartmentOfficeScheduleAssignmentDto>> SetDepartmentAsync(string companyId, string actorUserId, string departmentId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(actorUserId)) return Fail<DepartmentOfficeScheduleAssignmentDto>("OFFICE_SCHEDULE_ACTOR_REQUIRED");
        if (!await departments.Exist(x => x.CompanyId == companyId && x.DepartmentId == departmentId && x.IsActive && !x.IsDeleted))
            return Fail<DepartmentOfficeScheduleAssignmentDto>("DEPARTMENT_NOT_FOUND");
        var scheduleId = request.ScheduleId?.Trim();
        var schedule = await ValidateSchedule(companyId, scheduleId);
        if (!schedule.Success) return Fail<DepartmentOfficeScheduleAssignmentDto>(schedule.Message);

        var existing = (await departmentAssignments.GetAll(x => x.CompanyId == companyId && x.Department == departmentId && x.IsActive && !x.IsDeleted, withDefaultFilter: false)).ToList();
        var current = existing.OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        if (current is not null && string.Equals(current.ScheduleId, scheduleId, StringComparison.Ordinal))
            return new Result<DepartmentOfficeScheduleAssignmentDto>
            {
                Success = true,
                MethodResult = new() { DepartmentId = departmentId, ScheduleId = scheduleId, ScheduleName = schedule.MethodResult?.Name }
            };

        var now = DateTime.UtcNow;
        if (existing.Count > 0)
        {
            var close = await departmentAssignments.UpdateMany(
                Builders<DepartmentScheduleAssignment>.Filter.Where(x => x.CompanyId == companyId && x.Department == departmentId && x.IsActive && !x.IsDeleted),
                Builders<DepartmentScheduleAssignment>.Update.Set(x => x.IsActive, false).Set(x => x.EffectiveTo, now.Date.AddDays(-1)).Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, now));
            if (!close.Success) return Fail<DepartmentOfficeScheduleAssignmentDto>("OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED");
        }

        if (!string.IsNullOrWhiteSpace(scheduleId))
        {
            var insert = await departmentAssignments.AddOne(new DepartmentScheduleAssignment
            {
                AssignmentId = Guid.NewGuid().ToString(), CompanyId = companyId, Department = departmentId, ScheduleId = scheduleId,
                EffectiveFrom = now.Date, IsActive = true, CreatedBy = actorUserId, CreatedDate = now
            });
            if (!insert.Success) return Fail<DepartmentOfficeScheduleAssignmentDto>("OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED");
        }
        return new Result<DepartmentOfficeScheduleAssignmentDto> { Success = true, MethodResult = new() { DepartmentId = departmentId, ScheduleId = scheduleId, ScheduleName = schedule.MethodResult?.Name } };
    }

    private Task<EmpUser?> Employee(string companyId, string userId) => employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == userId && x.Status && !x.IsDeleted);
    private Task<CompanyOfficeSchedule?> Schedule(string companyId, string scheduleId) => schedules.FirstOrDefault(x => x.CompanyId == companyId && x.ScheduleId == scheduleId && x.IsActive && !x.IsDeleted);
    private async Task<Result<CompanyOfficeSchedule>> ValidateSchedule(string companyId, string? scheduleId)
    {
        if (string.IsNullOrWhiteSpace(scheduleId)) return new Result<CompanyOfficeSchedule> { Success = true };
        var schedule = await Schedule(companyId, scheduleId);
        return schedule is null ? Fail<CompanyOfficeSchedule>("OFFICE_SCHEDULE_NOT_FOUND") : new Result<CompanyOfficeSchedule> { Success = true, MethodResult = schedule };
    }
    private static Result<T> Fail<T>(string message) => new() { Success = false, Message = message };
}
