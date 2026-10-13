using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Attendance;

public sealed record AttendancePayrollReadiness(bool IsReady, IReadOnlyList<string> BlockingReasons);
public interface IAttendancePayrollReadinessService { Task<AttendancePayrollReadiness> EvaluateAsync(string companyId, DateTime payMonth, IReadOnlyCollection<string>? employeeIds = null); }

/// <summary>Single payroll gate for unresolved correction requests in the processed month.</summary>
public sealed class AttendancePayrollReadinessService(
    IMongoDbRepository<AttendanceCorrectionRequest> corrections,
    IMongoDbRepository<EmpUser> employees) : IAttendancePayrollReadinessService
{
    public async Task<AttendancePayrollReadiness> EvaluateAsync(string companyId, DateTime payMonth, IReadOnlyCollection<string>? employeeIds = null)
    {
        var start = new DateTime(payMonth.Year, payMonth.Month, 1); var end = start.AddMonths(1);
        var selected = employeeIds?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pending = await corrections.GetAll(x => x.CompanyId == companyId && x.AttendanceDate >= start && x.AttendanceDate < end && x.Status == "Pending");
        var userIds = pending.Where(x => selected is null || selected.Contains(x.UserId)).Select(x => x.UserId).Distinct().ToArray();
        if (userIds.Length == 0) return new AttendancePayrollReadiness(true, []);
        var people = await employees.GetAll(x => x.CompanyId == companyId && userIds.Contains(x.UserId));
        var names = people.Select(x => string.IsNullOrWhiteSpace(x.EmployeeId) ? x.UserId : x.EmployeeId).OrderBy(x => x).ToArray();
        return new AttendancePayrollReadiness(false, names.Select(x => $"{x}: unresolved attendance correction request").ToArray());
    }
}
