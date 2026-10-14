using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;

public static class AttendancePayrollRules
{
    public static int ExceededOccurrences(int lhdCount, int edCount, int allowedCount) =>
        Math.Max(0, lhdCount + edCount - Math.Max(0, allowedCount));

    public static bool TryGetEligiblePeriod(EmpUser employee, DateTime monthStart, DateTime monthEnd, out DateTime eligibleFrom, out DateTime eligibleTo)
    {
        eligibleFrom = monthStart.Date;
        eligibleTo = monthEnd.Date;
        if (!EmploymentPeriod.TryGetJoiningDate(employee, out var joining) || joining.Date > monthEnd.Date) return false;
        eligibleFrom = joining.Date > monthStart.Date ? joining.Date : monthStart.Date;
        var exitDate = EmploymentPeriod.GetLastWorkingDate(employee);
        if (exitDate.HasValue)
        {
            if (exitDate.Value < monthStart.Date) return false;
            eligibleTo = exitDate.Value < monthEnd.Date ? exitDate.Value : monthEnd.Date;
        }
        return eligibleFrom <= eligibleTo;
    }

    public static bool IsEligibleForPayrollMonth(EmpUser employee, DateTime monthStart) =>
        EmploymentPeriod.IsPayrollIncludedForMonth(employee, monthStart);
}
