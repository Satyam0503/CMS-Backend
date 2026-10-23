using System;
using System.Collections.Generic;
using System.Linq;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;

public static class AttendancePayrollRules
{
    public static int ExceededOccurrences(int lhdCount, int edCount, int allowedCount) =>
        Math.Max(0, lhdCount + edCount - Math.Max(0, allowedCount));

    public static int ExceededOccurrences(int combinedCount, int allowedCount) =>
        Math.Max(0, combinedCount - Math.Max(0, allowedCount));

    public static bool IsPunctualityInfraction(string? status)
    {
        var s = status?.Trim().ToUpperInvariant();
        return s is "LHD" or "ED" or "LHD+ED";
    }

    /// <summary>
    /// Returns the occurrence weight towards the combined punctuality limit:
    /// - "LHD+ED" counts as 2 (both Late Half Day and Early Departure occurred on this day)
    /// - "LHD" counts as 1
    /// - "ED" counts as 1
    /// - Other statuses count as 0
    /// </summary>
    public static int GetPunctualityWeight(string? status)
    {
        var s = status?.Trim().ToUpperInvariant();
        return s switch
        {
            "LHD+ED" => 2,
            "LHD" or "ED" => 1,
            _ => 0
        };
    }

    /// <summary>
    /// Calculates the total combined punctuality occurrences across statuses,
    /// where LHD counts as 1, ED counts as 1, and LHD+ED counts as 2.
    /// </summary>
    public static int CalculateCombinedPunctualityCount(IEnumerable<string?> statuses) =>
        statuses.Sum(GetPunctualityWeight);

    /// <summary>
    /// Resolves the set of calendar dates (sorted chronologically) that fall completely within the employee's
    /// allowed combined LHD + ED monthly grace limit.
    /// Each day with LHD or ED consumes 1 occurrence from the grace limit; a day with LHD+ED consumes 2 occurrences.
    /// Dates in this set are excused from punctuality unpaid salary deductions (100% paid).
    /// </summary>
    public static HashSet<DateTime> ResolveGraceExemptDates(
        IEnumerable<(DateTime Date, string? Status)> infractions,
        int allowedGraceLimit)
    {
        if (allowedGraceLimit <= 0) return [];
        var exemptDates = new HashSet<DateTime>();
        var runningCount = 0;

        var dailyInfractions = infractions
            .GroupBy(x => x.Date.Date)
            .OrderBy(g => g.Key);

        foreach (var group in dailyInfractions)
        {
            var statuses = group.Select(x => x.Status?.Trim().ToUpperInvariant()).ToList();
            var weight = (statuses.Contains("LHD+ED") || (statuses.Contains("LHD") && statuses.Contains("ED")))
                ? 2
                : statuses.Any(s => s is "LHD" or "ED") ? 1 : 0;

            if (weight <= 0) continue;

            if (runningCount + weight <= allowedGraceLimit)
            {
                exemptDates.Add(group.Key);
                runningCount += weight;
            }
            else
            {
                // Grace limit is exceeded or exhausted on this date
                break;
            }
        }

        return exemptDates;
    }

    /// <summary>
    /// Fallback overload for date-only sequences, treating each infraction date as 1 occurrence.
    /// </summary>
    public static HashSet<DateTime> ResolveGraceExemptDates(IEnumerable<DateTime> infractionDates, int allowedGraceLimit)
    {
        if (allowedGraceLimit <= 0) return [];
        return infractionDates
            .Select(d => d.Date)
            .Distinct()
            .OrderBy(d => d)
            .Take(allowedGraceLimit)
            .ToHashSet();
    }

    /// <summary>
    /// Computes the effective unpaid day fraction for an attendance record.
    /// If the day falls within the monthly grace limit, LHD / LHD+ED is excused (0 unpaid).
    /// </summary>
    public static decimal ResolveEffectiveUnpaidDayFraction(string? status, bool isGraceExempt, decimal configuredUnpaidFraction)
    {
        var s = status?.Trim().ToUpperInvariant();
        if (isGraceExempt && s is "LHD" or "LHD+ED")
            return 0m;
        return configuredUnpaidFraction;
    }

    /// <summary>
    /// Computes the effective paid day fraction for an attendance record.
    /// If the day falls within the monthly grace limit, LHD / LHD+ED is fully paid (1.0).
    /// </summary>
    public static decimal ResolveEffectivePaidDayFraction(string? status, bool isGraceExempt, decimal configuredPaidFraction)
    {
        var s = status?.Trim().ToUpperInvariant();
        if (isGraceExempt && s is "LHD" or "LHD+ED")
            return 1m;
        return configuredPaidFraction;
    }

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
