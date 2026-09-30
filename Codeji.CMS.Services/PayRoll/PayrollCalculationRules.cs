using System;

public static class PayrollCalculationRules
{
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
        "HD" or "LHD" or "WFH-HD" => 0.5m,
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
        decimal wageCeiling = 15000m)
    {
        var cleanBasic = Math.Max(0m, earnedBasicAfterLop);
        if (cleanBasic <= 0m) return 0m;

        var mode = string.IsNullOrWhiteSpace(epfMode) ? "CAPPED_CEILING" : epfMode.ToUpperInvariant();
        return mode switch
        {
            "EXEMPT" => 0m,
            "ACTUAL_BASIC_12" => RoundMoney(cleanBasic * 0.12m),
            _ => RoundMoney(Math.Min(cleanBasic, wageCeiling <= 0m ? 15000m : wageCeiling) * 0.12m)
        };
    }

    public static decimal CalculateMonthlyEsic(
        string? esicMode,
        decimal contractualFixedGross,
        decimal earnedMonthlyGrossAfterLop,
        decimal wageCeiling = 21000m)
    {
        var mode = string.IsNullOrWhiteSpace(esicMode) ? "STATUTORY" : esicMode.ToUpperInvariant();
        if (mode == "EXEMPT") return 0m;
        if (contractualFixedGross > (wageCeiling <= 0m ? 21000m : wageCeiling) || earnedMonthlyGrossAfterLop <= 0m) return 0m;

        // Statutory ESIC employee contribution is 0.75% of gross wages, rounded up to the next rupee.
        return Math.Ceiling(earnedMonthlyGrossAfterLop * 0.0075m);
    }

    public static decimal CalculateEmployerMonthlyEsic(string? esicMode, decimal contractualGross, decimal earnedMonthlyGrossAfterLop, decimal wageCeiling = 21000m)
    {
        var mode = string.IsNullOrWhiteSpace(esicMode) ? "STATUTORY" : esicMode.ToUpperInvariant();
        if (mode == "EXEMPT" || contractualGross > (wageCeiling <= 0m ? 21000m : wageCeiling) || earnedMonthlyGrossAfterLop <= 0m) return 0m;
        return Math.Ceiling(earnedMonthlyGrossAfterLop * 0.0325m);
    }
    public static decimal CalculateAnnualIncomeTaxWithCess(
        decimal annualGrossIncome,
        string? tdsMode = "AUTO_NEW_REGIME")
    {
        var mode = string.IsNullOrWhiteSpace(tdsMode) ? "AUTO_NEW_REGIME" : tdsMode.ToUpperInvariant();
        if (mode == "MANUAL_ONLY" || annualGrossIncome <= 0m)
            return 0m;

        decimal tax;
        if (mode == "AUTO_OLD_REGIME")
        {
            const decimal oldRegimeStandardDeduction = 50000m;
            decimal taxableIncome = Math.Max(0m, annualGrossIncome - oldRegimeStandardDeduction);

            if (taxableIncome <= 500000m)
                return 0m;

            if (taxableIncome <= 1000000m)
                tax = (250000m * 0.05m) + ((taxableIncome - 500000m) * 0.20m);
            else
                tax = (250000m * 0.05m) + (500000m * 0.20m) + ((taxableIncome - 1000000m) * 0.30m);
        }
        else
        {
            // Default: Section 115BAC New Tax Regime (FY 2025-26 / FY 2026-27)
            // Standard Deduction u/s 16(ia): ₹75,000
            const decimal newRegimeStandardDeduction = 75000m;
            const decimal section87ARebateThreshold = 1200000m;
            decimal taxableIncome = Math.Max(0m, annualGrossIncome - newRegimeStandardDeduction);

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
        }

        // 4% Health & Education Cess
        decimal totalAnnualTax = tax + (tax * 0.04m);
        return RoundMoney(totalAnnualTax);
    }

    public static decimal CalculateMonthlyTds(
        decimal annualGrossIncome,
        string? tdsMode = "AUTO_NEW_REGIME",
        decimal ytdTaxAlreadyDeducted = 0m,
        int remainingMonthsInFy = 12)
    {
        var annualTax = CalculateAnnualIncomeTaxWithCess(annualGrossIncome, tdsMode);
        if (annualTax <= 0m) return 0m;

        var remainingTax = Math.Max(0m, annualTax - Math.Max(0m, ytdTaxAlreadyDeducted));
        var divisorMonths = Math.Clamp(remainingMonthsInFy, 1, 12);
        return RoundMoney(remainingTax / divisorMonths);
    }
}
