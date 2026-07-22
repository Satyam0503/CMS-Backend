using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using MongoDB.Driver;

public interface ILeaveAttendanceReconciliationService
{
    Task<Result> ReconcileAcceptedLeaveAsync(string companyId, string leaveRequestId, string actorUserId, CancellationToken cancellationToken = default);
    Task<Result> ReverseLeaveAttendanceAsync(string companyId, string leaveRequestId, int expectedSourceVersion, string actorUserId, CancellationToken cancellationToken = default);
}

public sealed class LeaveAttendanceReconciliationService(
    IMongoDbRepository<LeaveRequest> leaves,
    IMongoDbRepository<LeavePolicy> policies,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceStatusSetting> statuses,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<AttendancePayrollException> exceptions,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    ICompanyWorkingCalendarService workingCalendar) : ILeaveAttendanceReconciliationService
{
    public async Task<Result> ReconcileAcceptedLeaveAsync(string companyId, string leaveRequestId, string actorUserId, CancellationToken cancellationToken = default)
    {
        var result = new Result { Success = false };
        var request = await leaves.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId);
        if (request == null || request.Status != Codeji.CMS.Utility.Enums.EnumsHelper.LeaveRequestStatus.Accepted) return result;

        var employee = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeId && x.Status);
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == request.LeavePolicyId && x.Status);
        if (employee == null || policy == null || string.IsNullOrWhiteSpace(policy.AttendanceStatusCode)) return result;

        var statusCode = policy.AttendanceStatusCode.Trim().ToUpperInvariant();
        var status = await statuses.FirstOrDefault(x => x.CompanyId == companyId && x.Code == statusCode && x.IsActive);
        if (status == null || status.RequiresTime) return result;

        var dates = await workingCalendar.GetLeaveDatesAsync(companyId, DateOnly.FromDateTime(request.StartDate),
            DateOnly.FromDateTime(request.EndDate), policy.WeekendInclusive, policy.HolidayInclusive, cancellationToken);

        foreach (var day in dates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = day.ToDateTime(TimeOnly.MinValue);
            var existing = await attendance.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existing != null && !(existing.SourceType == "LEAVE" && existing.SourceId == request.LeaveRequestId))
            {
                var month = new DateTime(date.Year, date.Month, 1);
                var conflict = await exceptions.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_ATTENDANCE_CONFLICT" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId);
                if (conflict == null)
                    await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_ATTENDANCE_CONFLICT", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, ExistingAttendanceId = existing.AttendanceId, ExistingStatus = existing.Status, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = "Approved leave conflicts with existing attendance.", CreatedBy = actorUserId });
                continue;
            }

            var entity = existing ?? new AttendanceModel { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, Date = date };
            entity.Status = statusCode;
            entity.SourceType = "LEAVE";
            entity.SourceId = request.LeaveRequestId;
            entity.SourceVersion = request.Version;
            entity.CheckInTime = null;
            entity.CheckOutTime = null;
            entity.TotalHours = null;
            entity.Remarks = $"Generated from approved leave {request.LeaveRequestId}";
            if (existing == null) await attendance.AddOne(entity);
            else await attendance.Update(Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, existing.AttendanceId), entity);
        }

        var affectedMonths = dates.Select(x => new DateTime(x.Year, x.Month, 1)).Distinct().ToList();
        foreach (var month in affectedMonths)
            await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && !x.IsLocked));
        result.Success = true;
        return result;
    }

    public async Task<Result> ReverseLeaveAttendanceAsync(string companyId, string leaveRequestId, int expectedSourceVersion, string actorUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filter = Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == companyId && x.SourceType == "LEAVE" && x.SourceId == leaveRequestId && x.SourceVersion <= expectedSourceVersion);
        return await attendance.DeleteAll(filter);
    }
}
