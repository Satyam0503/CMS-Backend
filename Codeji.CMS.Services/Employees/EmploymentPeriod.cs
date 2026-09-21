using System.Globalization;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Employees;

/// <summary>
/// Central employment-period rules used by attendance and payroll. A completed
/// exit must not erase the employee's entitlement for the month in which they
/// worked, while future months must remain excluded.
/// </summary>
public static class EmploymentPeriod
{
    public static DateTime? GetLastWorkingDate(EmpUser employee)
    {
        if (employee.LastWorkingDate.HasValue)
            return employee.LastWorkingDate.Value.Date;

        return DateTime.TryParse(employee.ExitDate, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out var legacyExitDate)
            ? legacyExitDate.Date
            : null;
    }

    public static bool IsEligibleForMonth(EmpUser employee, DateTime month)
    {
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        if (TryGetJoiningDate(employee, out var joiningDate) && joiningDate > monthEnd)
            return false;

        var lastWorkingDate = GetLastWorkingDate(employee);
        // Older records may only have ExitDate and Status=false. Treat those
        // as completed exits too, so the migration can be deployed safely.
        var isCompletedExit = EmploymentExitRules.IsFormerEmployee(employee) || (!employee.Status && lastWorkingDate.HasValue);
        if (!isCompletedExit)
            return employee.Status;

        return lastWorkingDate.HasValue && lastWorkingDate.Value >= monthStart;
    }

    public static bool WasEmployedOn(EmpUser employee, DateTime date)
    {
        var day = date.Date;
        if (TryGetJoiningDate(employee, out var joiningDate) && joiningDate > day)
            return false;

        var lastWorkingDate = GetLastWorkingDate(employee);
        var isCompletedExit = EmploymentExitRules.IsFormerEmployee(employee) || (!employee.Status && lastWorkingDate.HasValue);
        if (!isCompletedExit)
            return employee.Status;

        return lastWorkingDate.HasValue && lastWorkingDate.Value >= day;
    }

    /// <summary>
    /// Returns whether the employee should receive a normal attendance row on a
    /// date. Employment and attendance expectation are intentionally separate:
    /// approved leave remains workflow-owned, while sabbatical, suspension, and
    /// long-term absence suppress normal attendance generation.
    /// </summary>
    public static bool IsAttendanceExpectedOn(EmpUser employee, DateTime date)
    {
        if (!WasEmployedOn(employee, date)) return false;
        return ResolveAvailabilityOn(employee, date) == EmployeeLifecycleStatuses.Working;
    }

    /// <summary>Resolves the effective availability without mutating the employee.</summary>
    public static string ResolveAvailabilityOn(EmpUser employee, DateTime date)
    {
        if (EmploymentExitRules.IsFormerEmployee(employee))
            return EmployeeLifecycleStatuses.NotApplicable;

        var lifecycle = employee.Lifecycle ?? new EmployeeLifecycleState();
        if (lifecycle.AvailabilityExpectedReturnDate.HasValue &&
            date.Date >= lifecycle.AvailabilityExpectedReturnDate.Value.Date &&
            lifecycle.AvailabilityStatus is not EmployeeLifecycleStatuses.Working)
            return EmployeeLifecycleStatuses.Working;
        return lifecycle.AvailabilityStatus;
    }

    /// <summary>
    /// Payroll may still include an employee in the month containing their last
    /// working date, but must not create future payroll for a lifecycle-closed
    /// account. Temporary availability states remain payroll-calculable so the
    /// attendance/leave rules determine paid versus unpaid days.
    /// </summary>
    public static bool IsPayrollIncludedForMonth(EmpUser employee, DateTime month)
    {
        if (!IsEligibleForMonth(employee, month)) return false;
        var payrollStatus = employee.Lifecycle?.PayrollStatus;
        var isFormer = EmploymentExitRules.IsFormerEmployee(employee);
        if (!isFormer && payrollStatus is not null && payrollStatus is not EmployeeLifecycleStatuses.PayrollActive and not EmployeeLifecycleStatuses.PayrollFinalSettlement)
            return false;
        if (!string.Equals(payrollStatus, EmployeeLifecycleStatuses.PayrollClosed, StringComparison.OrdinalIgnoreCase)) return true;
        var lastWorkingDate = GetLastWorkingDate(employee);
        return lastWorkingDate.HasValue && lastWorkingDate.Value >= new DateTime(month.Year, month.Month, 1);
    }

    private static bool TryGetJoiningDate(EmpUser employee, out DateTime joiningDate) =>
        DateTime.TryParse(employee.DateOfJoining, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out joiningDate);
}
