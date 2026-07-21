using Codeji.CMS.Repository.Entities.Employees;

public static class AttendancePayrollRules
{
    public static int ExceededOccurrences(int lhdCount, int edCount, int allowedCount) =>
        Math.Max(0, lhdCount + edCount - Math.Max(0, allowedCount));

    public static bool TryGetEligiblePeriod(EmpUser employee, DateTime monthStart, DateTime monthEnd, out DateTime eligibleFrom, out DateTime eligibleTo)
    {
        eligibleFrom = monthStart.Date;
        eligibleTo = monthEnd.Date;
        if (!DateTime.TryParse(employee.DateOfJoining, out var joining) || joining.Date > monthEnd.Date) return false;
        eligibleFrom = joining.Date > monthStart.Date ? joining.Date : monthStart.Date;
        if (DateTime.TryParse(employee.ExitDate, out var exit))
        {
            if (exit.Date < monthStart.Date) return false;
            eligibleTo = exit.Date < monthEnd.Date ? exit.Date : monthEnd.Date;
        }
        return eligibleFrom <= eligibleTo;
    }
}
