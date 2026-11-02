using System;
using Codeji.CMS.Repository.Entities.Employees;

public static class PayrollCalculationRules
{
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

    public static decimal CalculateMonthlyEpf(
        string? epfMode,
        decimal earnedBasicAfterLop,
        decimal wageCeiling = 15000m,
        decimal contributionRatePercent = 12m)
    {
        var cleanBasic = Math.Max(0m, earnedBasicAfterLop);
        if (cleanBasic <= 0m) return 0m;

        var mode = string.IsNullOrWhiteSpace(epfMode) ? "CAPPED_CEILING" : epfMode.ToUpperInvariant();
        var contributionBase = mode switch
        {
            "EXEMPT" => 0m,
            "ACTUAL_BASIC_12" => cleanBasic,
            _ => Math.Min(cleanBasic, wageCeiling <= 0m ? 15000m : wageCeiling)
        };
        return RoundMoney(contributionBase * contributionRatePercent / 100m);
    }

    public static (decimal EmployerEpf, decimal EmployerEps, decimal Edli, decimal AdminCharges) CalculateEmployerMonthlyEpf(
        string? epfMode,
        decimal earnedBasicAfterLop,
        decimal wageCeiling = 15000m,
        decimal epsCeiling = 15000m)
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
        var eps = RoundMoney(Math.Min(epsWage * 8.33m / 100m, 1250m));
        var totalEmployerPf = RoundMoney(pfWage * 12m / 100m);
        var epf = Math.Max(0m, totalEmployerPf - eps);
        var edli = RoundMoney(Math.Min(pfWage, 15000m) * 0.50m / 100m);
        var admin = RoundMoney(pfWage * 0.50m / 100m);

        return (epf, eps, edli, admin);
    }

    public static decimal CalculateMonthlyEsic(
        string? esicMode,
        decimal contractualFixedGross,
        decimal earnedMonthlyGrossAfterLop,
        decimal wageCeiling = 21000m,
        decimal contributionRatePercent = 0.75m)
    {
        var mode = string.IsNullOrWhiteSpace(esicMode) ? "STATUTORY" : esicMode.ToUpperInvariant();
        if (mode == "EXEMPT") return 0m;
        if (contractualFixedGross > (wageCeiling <= 0m ? 21000m : wageCeiling) || earnedMonthlyGrossAfterLop <= 0m) return 0m;

        // Statutory ESIC employee contribution is 0.75% of gross wages, rounded up to the next rupee.
        return Math.Ceiling(earnedMonthlyGrossAfterLop * contributionRatePercent / 100m);
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

    public static decimal CalculateEmployerMonthlyEsic(string? esicMode, decimal contractualGross, decimal earnedMonthlyGrossAfterLop, decimal wageCeiling = 21000m, decimal contributionRatePercent = 3.25m)
    {
        var mode = string.IsNullOrWhiteSpace(esicMode) ? "STATUTORY" : esicMode.ToUpperInvariant();
        if (mode == "EXEMPT" || contractualGross > (wageCeiling <= 0m ? 21000m : wageCeiling) || earnedMonthlyGrossAfterLop <= 0m) return 0m;
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
        decimal otherApprovedDeductions = 0m)
    {
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
            const decimal oldRegimeStandardDeduction = 50000m;
            var capped80C = Math.Min(150000m, Math.Max(0m, approved80C));
            var capped80Ccd1B = Math.Min(50000m, Math.Max(0m, approved80Ccd1B));
            var capped80D = Math.Min(100000m, Math.Max(0m, approved80D));
            var cappedSection24 = Math.Min(200000m, Math.Max(0m, approvedSection24Loss));
            var hraExemption = Math.Max(0m, approvedHraExemption);
            var otherDeductions = Math.Max(0m, otherApprovedDeductions);

            var totalDeductions = oldRegimeStandardDeduction + capped80C + capped80Ccd1B + capped80D + cappedSection24 + hraExemption + otherDeductions;
            taxableIncome = Math.Max(0m, totalGrossIncome - totalDeductions);

            if (taxableIncome <= 500000m)
                return 0m; // Full rebate u/s 87A under Old Regime up to ₹5,00,000

            if (taxableIncome <= 1000000m)
                tax = (250000m * 0.05m) + ((taxableIncome - 500000m) * 0.20m);
            else
                tax = (250000m * 0.05m) + (500000m * 0.20m) + ((taxableIncome - 1000000m) * 0.30m);

            // Surcharge under Old Regime
            if (taxableIncome > 50000000m)
                tax += tax * 0.37m;
            else if (taxableIncome > 20000000m)
                tax += tax * 0.25m;
            else if (taxableIncome > 10000000m)
                tax += tax * 0.15m;
            else if (taxableIncome > 5000000m)
                tax += tax * 0.10m;
        }
        else
        {
            // Section 115BAC New Tax Regime (FY 2025-26 / FY 2026-27)
            // Standard Deduction u/s 16(ia): ₹75,000
            const decimal newRegimeStandardDeduction = 75000m;
            const decimal section87ARebateThreshold = 1200000m;
            taxableIncome = Math.Max(0m, totalGrossIncome - newRegimeStandardDeduction);

            // Full Section 87A tax rebate up to ₹12,00,000 taxable income (₹12,75,000 gross salary)
            if (taxableIncome <= section87ARebateThreshold)
                return 0m;

            decimal slabTax = 0m;
            if (taxableIncome <= 400000m)
                slabTax = 0m;
            else if (taxableIncome <= 800000m)
                slabTax = (taxableIncome - 400000m) * 0.05m;
            else if (taxableIncome <= 1200000m)
                slabTax = 20000m + ((taxableIncome - 800000m) * 0.10m);
            else if (taxableIncome <= 1600000m)
                slabTax = 60000m + ((taxableIncome - 1200000m) * 0.15m);
            else if (taxableIncome <= 2000000m)
                slabTax = 120000m + ((taxableIncome - 1600000m) * 0.20m);
            else if (taxableIncome <= 2400000m)
                slabTax = 200000m + ((taxableIncome - 2000000m) * 0.25m);
            else
                slabTax = 300000m + ((taxableIncome - 2400000m) * 0.30m);

            // Marginal Relief u/s 87A: tax payable cannot exceed the income exceeding ₹12,00,000
            decimal excessIncomeOverRebateThreshold = taxableIncome - section87ARebateThreshold;
            tax = Math.Min(slabTax, excessIncomeOverRebateThreshold);

            // Surcharge under New Regime (capped at 25% for > 2Cr)
            if (taxableIncome > 20000000m)
                tax += tax * 0.25m;
            else if (taxableIncome > 10000000m)
                tax += tax * 0.15m;
            else if (taxableIncome > 5000000m)
                tax += tax * 0.10m;
        }

        // 4% Health & Education Cess
        decimal totalAnnualTax = tax + (tax * 0.04m);
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
        decimal otherApprovedDeductions = 0m)
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
            otherApprovedDeductions);

        if (annualTax <= 0m) return 0m;

        var totalTaxAlreadyDeducted = Math.Max(0m, ytdTaxAlreadyDeducted) + Math.Max(0m, previousEmployerTds);
        var remainingTax = Math.Max(0m, annualTax - totalTaxAlreadyDeducted);
        var divisorMonths = Math.Clamp(remainingMonthsInFy, 1, 12);
        return RoundMoney(remainingTax / divisorMonths);
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
