using System;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

public static class PayrollCalculationRules
{
    public const string IndiaEpfRulesBefore2026_09_17 = "IN-EPF-2025-15000";
    public const string IndiaEpfRulesFrom2026_09_17 = "IN-EPF-2026-25000";
    private static readonly DateTime IndiaEpfCeilingEffectiveDate = new(2026, 9, 17);

    public static (string RuleSetId, decimal WageCeiling, decimal EpsWageCeiling, decimal EdliWageCeiling)
        ResolveIndiaEpfRules(DateTime payrollPeriodEnd)
    {
        // Statutory ceiling is selected by the contribution period, not by today's date.
        // Company policy continues to supply contribution rates and membership mode.
        return payrollPeriodEnd.Date >= IndiaEpfCeilingEffectiveDate
            ? (IndiaEpfRulesFrom2026_09_17, 25000m, 25000m, 25000m)
            : (IndiaEpfRulesBefore2026_09_17, 15000m, 15000m, 15000m);
    }

    private static readonly HashSet<string> SupportedTdsModes = new(StringComparer.Ordinal)
    {
        "AUTO_NEW_REGIME", "AUTO_OLD_REGIME", "MANUAL_ONLY", "NEW_115BAC", "OLD_REGIME"
    };

    public static bool IsSupportedTdsMode(string? tdsMode) =>
        SupportedTdsModes.Contains(string.IsNullOrWhiteSpace(tdsMode) ? "AUTO_NEW_REGIME" : tdsMode.Trim().ToUpperInvariant());

    public static string? FindUnsupportedAutomaticTdsTreatment(
        IEnumerable<PayrollComponentSnapshot> components, string? tdsMode)
    {
        var mode = string.IsNullOrWhiteSpace(tdsMode) ? "AUTO_NEW_REGIME" : tdsMode.Trim().ToUpperInvariant();
        if (mode == "MANUAL_ONLY") return null;
        var unsupported = components.FirstOrDefault(component =>
            string.Equals(component.Kind, "EARNING", StringComparison.OrdinalIgnoreCase) && !component.IsTaxable);
        return unsupported is null
            ? null
            : $"Automatic TDS cannot safely calculate non-taxable earning '{unsupported.Name}' ({unsupported.Code}) because this payroll version does not capture exemption category, eligibility, or evidence. Resolve the tax treatment with payroll/legal review before processing this employee.";
    }

    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    public static decimal ResolveDivisor(string policy, int calendarDays, decimal eligibleWorkingDays) => policy switch
    {
        "FIXED_30_DAYS" => 30m,
        "WORKING_DAYS" => eligibleWorkingDays,
        _ => calendarDays
    };

    // Business decision: configured status unpaid time remains the attendance treatment;
    // an approved exceeded-limit penalty is additional and is supplied exactly once by
    // the single idempotent monthly source exception.
    public static decimal ResolveFallbackUnpaidDayFraction(string? status) => status?.Trim().ToUpperInvariant() switch
    {
        "A" => 1m,
        "HD" or "LHD" or "LHD+ED" or "WFH-HD" => 0.5m,
        _ => 0m
    };

    public static decimal TotalLossOfPayDays(decimal attendanceUnpaidDays, decimal approvedPolicyPenaltyDays) =>
        attendanceUnpaidDays + approvedPolicyPenaltyDays;

    public static decimal ResolvePerDayLopRate(
        string? lopCalculationBase,
        decimal contractualBasic,
        decimal contractualFixedGross,
        decimal resolvedDivisor)
    {
        if (resolvedDivisor <= 0) return 0m;
        var baseAmount = string.Equals(lopCalculationBase, "BASIC_ONLY", StringComparison.OrdinalIgnoreCase)
            ? contractualBasic
            : contractualFixedGross;
        return baseAmount / resolvedDivisor;
    }

    public static decimal CalculateCappedLossOfPay(
        decimal lossOfPayDays,
        decimal perDaySalary,
        decimal earnedFixedGross)
    {
        if (lossOfPayDays <= 0 || perDaySalary <= 0 || earnedFixedGross <= 0)
            return 0m;
        var rawLop = RoundMoney(lossOfPayDays * perDaySalary);
        return Math.Min(earnedFixedGross, rawLop);
    }

    public static decimal CalculateLopAdjustedWageBase(decimal monthlyWageBase, decimal lossOfPayDays, decimal divisor)
    {
        if (monthlyWageBase <= 0m || lossOfPayDays <= 0m) return Math.Max(0m, monthlyWageBase);
        if (divisor <= 0m) return 0m;
        var unpaidFraction = Math.Clamp(lossOfPayDays / divisor, 0m, 1m);
        return RoundMoney(Math.Max(0m, monthlyWageBase) * (1m - unpaidFraction));
    }

    public static decimal CalculateMonthlyEpf(
        string? epfMode,
        decimal earnedBasicAfterLop,
        decimal wageCeiling = 15000m,
        decimal contributionRatePercent = 12m)
    {
        var cleanBasic = Math.Max(0m, earnedBasicAfterLop);
        if (cleanBasic <= 0m) return 0m;

        var contributionBase = ResolveEpfContributionWageBase(epfMode, cleanBasic, wageCeiling);
        // EPF Scheme, paragraph 29(4): round each member contribution to the nearest rupee.
        return Math.Round(contributionBase * contributionRatePercent / 100m, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal ResolveEpfContributionWageBase(string? epfMode, decimal earnedBasic, decimal wageCeiling)
    {
        var cleanBasic = Math.Max(0m, earnedBasic);
        var mode = string.IsNullOrWhiteSpace(epfMode) ? "CAPPED_CEILING" : epfMode.Trim().ToUpperInvariant();
        return mode switch
        {
            "EXEMPT" => 0m,
            "ACTUAL_BASIC_12" => cleanBasic,
            _ => Math.Min(cleanBasic, wageCeiling > 0m ? wageCeiling : 15000m)
        };
    }

    public static (decimal EmployerEpf, decimal EmployerEps, decimal Edli, decimal AdminCharges) CalculateEmployerMonthlyEpf(
        string? epfMode,
        decimal earnedBasicAfterLop,
        decimal wageCeiling = 15000m,
        decimal epsCeiling = 15000m,
        decimal employerContributionRatePercent = 12m)
    {
        var cleanBasic = Math.Max(0m, earnedBasicAfterLop);
        if (cleanBasic <= 0m) return (0m, 0m, 0m, 0m);

        var mode = string.IsNullOrWhiteSpace(epfMode) ? "CAPPED_CEILING" : epfMode.ToUpperInvariant();
        if (mode == "EXEMPT") return (0m, 0m, 0m, 0m);

        var effectiveWageCeiling = wageCeiling <= 0m ? 15000m : wageCeiling;
        var effectiveEpsCeiling = epsCeiling <= 0m ? 15000m : epsCeiling;

        var pfWage = mode switch
        {
            "ACTUAL_BASIC_12" => cleanBasic,
            _ => Math.Min(cleanBasic, effectiveWageCeiling)
        };

        var epsWage = Math.Min(cleanBasic, effectiveEpsCeiling);
        var eps = Math.Round(Math.Min(epsWage * 8.33m / 100m, effectiveEpsCeiling * 8.33m / 100m), 0, MidpointRounding.AwayFromZero);
        var totalEmployerPf = Math.Round(pfWage * employerContributionRatePercent / 100m, 0, MidpointRounding.AwayFromZero);
        var epf = Math.Max(0m, totalEmployerPf - eps);
        var edli = Math.Round(Math.Min(pfWage, effectiveEpsCeiling) * 0.50m / 100m, 0, MidpointRounding.AwayFromZero);
        // EPF administration charges are assessed at establishment level (including minimums),
        // so they must not be multiplied into an individual employee contribution snapshot.
        const decimal admin = 0m;

        return (epf, eps, edli, admin);
    }

    public static decimal CalculateMonthlyEsic(
        string? esicMode,
        decimal contractualFixedGross,
        decimal earnedMonthlyGrossAfterLop,
        decimal wageCeiling = 21000m,
        decimal contributionRatePercent = 0.75m,
        bool? contributionPeriodCoverageEligible = null)
    {
        var mode = string.IsNullOrWhiteSpace(esicMode) ? "STATUTORY" : esicMode.ToUpperInvariant();
        if (mode == "EXEMPT") return 0m;
        var isCovered = contributionPeriodCoverageEligible ?? contractualFixedGross <= (wageCeiling <= 0m ? 21000m : wageCeiling);
        if (!isCovered || earnedMonthlyGrossAfterLop <= 0m) return 0m;

        // Statutory ESIC employee contribution is 0.75% of gross wages, rounded up to the next rupee.
        return Math.Ceiling(earnedMonthlyGrossAfterLop * contributionRatePercent / 100m);
    }

    public static decimal ResolveEsicWageBasis(IEnumerable<SalaryWageComponent>? components, decimal legacyWageBasis)
    {
        var classifiedComponents = components?.ToArray() ?? [];
        var includedWages = classifiedComponents
            .Where(component => component.IncludeInEsiWage)
            .Sum(component => Math.Max(0m, component.Amount));

        // Older salary structures have no wage classification metadata. Preserve their
        // historical basis when metadata is absent or no component is classified yet.
        return includedWages > 0m ? includedWages : Math.Max(0m, legacyWageBasis);
    }

    public static (DateTime Start, DateTime End) ResolveIndiaEsicContributionPeriod(DateTime payrollMonth)
    {
        var month = new DateTime(payrollMonth.Year, payrollMonth.Month, 1);
        var start = month.Month >= 4 && month.Month <= 9
            ? new DateTime(month.Year, 4, 1)
            : month.Month >= 10
                ? new DateTime(month.Year, 10, 1)
                : new DateTime(month.Year - 1, 10, 1);
        return (start, start.AddMonths(6).AddDays(-1));
    }

    public static bool ResolveEsicCoverageEligibility(
        string? esicMode,
        decimal wagesAtCoverageStart,
        decimal wageCeiling,
        DateTime payrollMonth,
        IEnumerable<EmpPayRoll>? previousContributionPeriodRows)
    {
        if (string.Equals(esicMode, "EXEMPT", StringComparison.OrdinalIgnoreCase)) return false;

        var period = ResolveIndiaEsicContributionPeriod(payrollMonth);
        var monthStart = new DateTime(payrollMonth.Year, payrollMonth.Month, 1);
        var ceiling = wageCeiling > 0m ? wageCeiling : 21000m;
        var wasCoveredDuringPeriod = (previousContributionPeriodRows ?? [])
            .Where(row => row.PayMonth.Date >= period.Start && row.PayMonth.Date < monthStart)
            .Any(row =>
            {
                if (string.Equals(row.CalculationSnapshot?.EsicMode, "EXEMPT", StringComparison.OrdinalIgnoreCase))
                    return false;
                if (row.CalculationSnapshot?.EsicCoverageEligible == true)
                    return true;
                if (row.CalculationSnapshot?.EsicCoverageEligible == false)
                    return false;
                if (row.CalculationSnapshot?.EsicWages > 0m || row.Deduction?.ESIC > 0m)
                    return true;

                // Legacy payroll rows did not persist the coverage decision. Their fixed wage
                // fields provide a conservative evidence path for previous covered months.
                var legacyWages = Math.Max(0m, row.BasicPay + row.Bonus +
                    (row.Allowance?.HRA ?? 0m) + (row.Allowance?.OtherAllowance ?? 0m));
                return legacyWages > 0m && legacyWages <= ceiling;
            });

        return wasCoveredDuringPeriod || (wagesAtCoverageStart > 0m && wagesAtCoverageStart <= ceiling);
    }

    /// <summary>
    /// Calculates statutory gratuity under the Payment of Gratuity Act, 1972:
    /// Formula: (15 * Last Drawn Basic * Completed Years of Service) / 26
    /// Eligibility: Minimum 5 years of continuous service (4 years 240 days = 4.65 years).
    /// Statutory maximum ceiling: ₹20,00,000 (20 Lakhs).
    /// </summary>
    public static decimal CalculateGratuity(decimal lastDrawnBasic, DateTime dateOfJoining, DateTime lastWorkingDate)
    {
        if (lastDrawnBasic <= 0m || dateOfJoining == default || lastWorkingDate <= dateOfJoining) return 0m;
        var totalDays = (lastWorkingDate.Date - dateOfJoining.Date).TotalDays;
        var tenureYears = totalDays / 365.25;
        if (tenureYears < 4.65) return 0m;

        var completedYears = (int)Math.Round(tenureYears, MidpointRounding.AwayFromZero);
        if (completedYears <= 0) return 0m;

        var gratuity = (15m * lastDrawnBasic * completedYears) / 26m;
        return RoundMoney(Math.Min(gratuity, 2000000m));
    }

    /// <summary>
    /// Calculates leave encashment for unutilized paid/earned leave days:
    /// Formula: (Last Drawn Basic / Divisor) * Unutilized Leave Days
    /// Default Divisor: 30 days (or company policy divisor).
    /// </summary>
    public static decimal CalculateLeaveEncashment(decimal lastDrawnBasic, decimal unutilizedLeaveDays, decimal divisor = 30m)
    {
        if (lastDrawnBasic <= 0m || unutilizedLeaveDays <= 0m) return 0m;
        var effectiveDivisor = divisor <= 0m ? 30m : divisor;
        return RoundMoney((lastDrawnBasic / effectiveDivisor) * unutilizedLeaveDays);
    }

    /// <summary>
    /// Calculates notice shortfall recovery when an employee serves fewer days than required:
    /// Formula: ((Required Notice Days - Served Notice Days) * Monthly Gross) / 30
    /// </summary>
    public static decimal CalculateNoticeShortfallRecovery(int requiredNoticeDays, int servedNoticeDays, decimal monthlyGross, decimal divisor = 30m)
    {
        if (requiredNoticeDays <= servedNoticeDays || monthlyGross <= 0m) return 0m;
        var shortfallDays = requiredNoticeDays - servedNoticeDays;
        var effectiveDivisor = divisor <= 0m ? 30m : divisor;
        return RoundMoney((monthlyGross / effectiveDivisor) * shortfallDays);
    }

    public static decimal CalculateEmployerMonthlyEsic(string? esicMode, decimal contractualGross, decimal earnedMonthlyGrossAfterLop, decimal wageCeiling = 21000m, decimal contributionRatePercent = 3.25m, bool? contributionPeriodCoverageEligible = null)
    {
        var mode = string.IsNullOrWhiteSpace(esicMode) ? "STATUTORY" : esicMode.ToUpperInvariant();
        var isCovered = contributionPeriodCoverageEligible ?? contractualGross <= (wageCeiling <= 0m ? 21000m : wageCeiling);
        if (mode == "EXEMPT" || !isCovered || earnedMonthlyGrossAfterLop <= 0m) return 0m;
        return Math.Ceiling(earnedMonthlyGrossAfterLop * contributionRatePercent / 100m);
    }

    public static decimal CalculateAnnualIncomeTaxWithCess(
        decimal annualGrossIncome,
        string? tdsMode = "AUTO_NEW_REGIME",
        decimal previousEmployerGrossSalary = 0m,
        decimal otherTaxableIncome = 0m,
        decimal approved80C = 0m,
        decimal approved80Ccd1B = 0m,
        decimal approved80D = 0m,
        decimal approvedSection24Loss = 0m,
        decimal approvedHraExemption = 0m,
        decimal otherApprovedDeductions = 0m,
        IndiaSalaryTdsRuleSet? taxRules = null)
    {
        var rules = taxRules ?? IndiaSalaryTdsRuleSet.ResolveOrDefault();
        var rawMode = string.IsNullOrWhiteSpace(tdsMode) ? "AUTO_NEW_REGIME" : tdsMode.Trim().ToUpperInvariant();
        if (!SupportedTdsModes.Contains(rawMode))
            throw new ArgumentOutOfRangeException(nameof(tdsMode), tdsMode, "Unsupported TDS mode. Choose AUTO_NEW_REGIME, AUTO_OLD_REGIME, or MANUAL_ONLY.");
        if (rawMode == "MANUAL_ONLY")
            return 0m;

        var mode = rawMode switch
        {
            "OLD_REGIME" => "AUTO_OLD_REGIME",
            "NEW_115BAC" => "AUTO_NEW_REGIME",
            _ => rawMode
        };

        var totalGrossIncome = Math.Max(0m, annualGrossIncome) + Math.Max(0m, previousEmployerGrossSalary) + Math.Max(0m, otherTaxableIncome);
        if (totalGrossIncome <= 0m)
            return 0m;

        decimal tax;
        decimal taxableIncome;

        if (mode == "AUTO_OLD_REGIME")
        {
            var capped80C = Math.Min(rules.Section80CCap, Math.Max(0m, approved80C));
            var capped80Ccd1B = Math.Min(rules.Section80Ccd1BCap, Math.Max(0m, approved80Ccd1B));
            var capped80D = Math.Min(rules.Section80DCap, Math.Max(0m, approved80D));
            var cappedSection24 = Math.Min(rules.Section24LossCap, Math.Max(0m, approvedSection24Loss));
            var hraExemption = Math.Max(0m, approvedHraExemption);
            var otherDeductions = Math.Max(0m, otherApprovedDeductions);

            var totalDeductions = rules.OldRegimeStandardDeduction + capped80C + capped80Ccd1B + capped80D + cappedSection24 + hraExemption + otherDeductions;
            taxableIncome = Math.Max(0m, totalGrossIncome - totalDeductions);

            if (taxableIncome <= rules.OldRegimeRebateThreshold)
                return 0m; // Full rebate u/s 87A under Old Regime up to ₹5,00,000

            tax = CalculateSlabTax(taxableIncome, rules.OldRegimeSlabs);

            tax = ApplySurcharge(tax, taxableIncome, rules.OldRegimeSurcharges);
        }
        else
        {
            taxableIncome = Math.Max(0m, totalGrossIncome - rules.NewRegimeStandardDeduction);

            if (taxableIncome <= rules.NewRegimeRebateThreshold)
                return 0m;

            var slabTax = CalculateSlabTax(taxableIncome, rules.NewRegimeSlabs);

            // Marginal Relief u/s 87A: tax payable cannot exceed the income exceeding ₹12,00,000
            decimal excessIncomeOverRebateThreshold = taxableIncome - rules.NewRegimeRebateThreshold;
            tax = Math.Min(slabTax, excessIncomeOverRebateThreshold);

            tax = ApplySurcharge(tax, taxableIncome, rules.NewRegimeSurcharges, rules.NewRegimeSurchargeMaximum);
        }

        decimal totalAnnualTax = tax + (tax * rules.CessRate);
        return RoundMoney(totalAnnualTax);
    }

    public static decimal CalculateMonthlyTds(
        decimal annualGrossIncome,
        string? tdsMode = "AUTO_NEW_REGIME",
        decimal ytdTaxAlreadyDeducted = 0m,
        int remainingMonthsInFy = 12,
        decimal previousEmployerGrossSalary = 0m,
        decimal previousEmployerTds = 0m,
        decimal otherTaxableIncome = 0m,
        decimal approved80C = 0m,
        decimal approved80Ccd1B = 0m,
        decimal approved80D = 0m,
        decimal approvedSection24Loss = 0m,
        decimal approvedHraExemption = 0m,
        decimal otherApprovedDeductions = 0m,
        IndiaSalaryTdsRuleSet? taxRules = null)
    {
        var annualTax = CalculateAnnualIncomeTaxWithCess(
            annualGrossIncome,
            tdsMode,
            previousEmployerGrossSalary,
            otherTaxableIncome,
            approved80C,
            approved80Ccd1B,
            approved80D,
            approvedSection24Loss,
            approvedHraExemption,
            otherApprovedDeductions,
            taxRules);

        if (annualTax <= 0m) return 0m;

        var totalTaxAlreadyDeducted = Math.Max(0m, ytdTaxAlreadyDeducted) + Math.Max(0m, previousEmployerTds);
        var remainingTax = Math.Max(0m, annualTax - totalTaxAlreadyDeducted);
        var divisorMonths = Math.Clamp(remainingMonthsInFy, 1, 12);
        return RoundMoney(remainingTax / divisorMonths);
    }

    private static decimal CalculateSlabTax(decimal taxableIncome, IReadOnlyList<IndiaIncomeTaxSlab> slabs) =>
        slabs.Sum(slab =>
        {
            var upper = slab.UpperBound.HasValue ? Math.Min(taxableIncome, slab.UpperBound.Value) : taxableIncome;
            return upper <= slab.LowerBound ? 0m : (upper - slab.LowerBound) * slab.Rate;
        });

    private static decimal ApplySurcharge(decimal tax, decimal taxableIncome,
        IReadOnlyList<IndiaIncomeTaxSurcharge> surcharges, decimal maximumRate = decimal.MaxValue)
    {
        var rate = surcharges.Where(x => taxableIncome > x.IncomeThreshold)
            .Select(x => x.Rate).DefaultIfEmpty(0m).Max();
        return tax + tax * Math.Min(rate, maximumRate);
    }

    /// <summary>
    /// Calculates statutory HRA exemption under Section 10(13A) and Rule 2A:
    /// Least of:
    /// 1. Actual HRA received
    /// 2. Rent paid in excess of 10% of Basic salary
    /// 3. 50% of Basic salary (Metro: Delhi, Mumbai, Kolkata, Chennai) or 40% (Non-Metro)
    /// </summary>
    public static decimal CalculateStatutoryHraExemption(
        decimal annualBasic,
        decimal annualHraReceived,
        decimal annualRentPaid,
        bool isMetro)
    {
        if (annualBasic <= 0m || annualHraReceived <= 0m || annualRentPaid <= 0m)
            return 0m;

        var rentOver10PctOfBasic = Math.Max(0m, annualRentPaid - (0.10m * annualBasic));
        var salaryLimit = isMetro ? (0.50m * annualBasic) : (0.40m * annualBasic);
        var exemption = Math.Min(annualHraReceived, Math.Min(rentOver10PctOfBasic, salaryLimit));
        return RoundMoney(exemption);
    }

    public static decimal CalculateMonthlyProfessionalTax(
        string? stateOrJurisdiction,
        decimal monthlyEarnedGross,
        int payMonthNumber,
        string? gender = null,
        string? ptMode = "STATUTORY")
    {
        if (string.Equals(ptMode, "EXEMPT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ptMode, "DISABLED", StringComparison.OrdinalIgnoreCase))
            return 0m;

        if (string.IsNullOrWhiteSpace(stateOrJurisdiction) || monthlyEarnedGross <= 0m)
            return 0m;

        var normalized = stateOrJurisdiction.Trim().ToUpperInvariant();
        if (normalized.StartsWith("IN-", StringComparison.Ordinal))
            normalized = normalized[3..];

        return normalized switch
        {
            "MH" or "MAHARASHTRA" => CalculateMaharashtraPt(monthlyEarnedGross, payMonthNumber, gender),
            "KA" or "KARNATAKA" => CalculateKarnatakaPt(monthlyEarnedGross),
            "TG" or "TS" or "TELANGANA" => CalculateTelanganaPt(monthlyEarnedGross),
            "AP" or "ANDHRA PRADESH" or "ANDHRAPRADESH" => CalculateAndhraPradeshPt(monthlyEarnedGross),
            "GJ" or "GUJARAT" => CalculateGujaratPt(monthlyEarnedGross),
            "WB" or "WEST BENGAL" or "WESTBENGAL" => CalculateWestBengalPt(monthlyEarnedGross),
            "TN" or "TAMIL NADU" or "TAMILNADU" => CalculateTamilNaduPt(monthlyEarnedGross),
            "MP" or "MADHYA PRADESH" or "MADHYAPRADESH" => CalculateMadhyaPradeshPt(monthlyEarnedGross, payMonthNumber),
            _ => 0m
        };
    }

    public static decimal CalculateMaharashtraPt(decimal gross, int month, string? gender)
    {
        var isFemale = string.Equals(gender, "FEMALE", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(gender, "WOMAN", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(gender, "WOMEN", StringComparison.OrdinalIgnoreCase);

        if (isFemale && gross <= 25000m) return 0m;

        if (gross <= 7500m) return 0m;
        if (gross <= 10000m) return 175m;

        return month == 2 ? 300m : 200m;
    }

    public static decimal CalculateKarnatakaPt(decimal gross)
    {
        return gross > 25000m ? 200m : 0m;
    }

    public static decimal CalculateTelanganaPt(decimal gross)
    {
        if (gross <= 15000m) return 0m;
        if (gross <= 20000m) return 150m;
        return 200m;
    }

    public static decimal CalculateAndhraPradeshPt(decimal gross)
    {
        if (gross <= 15000m) return 0m;
        if (gross <= 20000m) return 150m;
        return 200m;
    }

    public static decimal CalculateGujaratPt(decimal gross)
    {
        return gross > 12000m ? 200m : 0m;
    }

    public static decimal CalculateWestBengalPt(decimal gross)
    {
        if (gross <= 10000m) return 0m;
        if (gross <= 15000m) return 110m;
        if (gross <= 25000m) return 130m;
        if (gross <= 40000m) return 150m;
        return 200m;
    }

    public static decimal CalculateTamilNaduPt(decimal gross)
    {
        if (gross <= 21000m) return 0m;
        if (gross <= 30000m) return 100m;
        if (gross <= 45000m) return 235m;
        if (gross <= 60000m) return 510m;
        if (gross <= 75000m) return 760m;
        return 1025m;
    }

    public static decimal CalculateMadhyaPradeshPt(decimal gross, int month)
    {
        if (gross <= 18750m) return 0m;
        if (gross <= 25000m) return month == 3 ? 135m : 125m;
        if (gross <= 33333m) return month == 3 ? 174m : 166m;
        return month == 3 ? 212m : 208m;
    }
}
