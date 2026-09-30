public class PayrollCalculationRulesTests
{
    [Theory]
    [InlineData("CALENDAR_DAYS", 31, 22, 31)]
    [InlineData("FIXED_30_DAYS", 31, 22, 30)]
    [InlineData("WORKING_DAYS", 31, 22, 22)]
    public void Resolves_configured_divisor(string policy, int calendar, decimal working, decimal expected) =>
        Assert.Equal(expected, PayrollCalculationRules.ResolveDivisor(policy, calendar, working));

    [Theory]
    [InlineData(0, 0.5, 0.5)]
    [InlineData(0.5, 0.5, 1)]
    [InlineData(1.5, 0, 1.5)]
    public void Approved_policy_penalty_is_added_once(decimal attendanceUnpaid, decimal penalty, decimal expected) =>
        Assert.Equal(expected, PayrollCalculationRules.TotalLossOfPayDays(attendanceUnpaid, penalty));

    [Theory]
    [InlineData("FIXED_GROSS", 50000, 100000, 30, 3333.33)]
    [InlineData("BASIC_ONLY", 50000, 100000, 30, 1666.67)]
    public void Resolves_per_day_lop_rate_from_configured_base(
        string lopBase,
        decimal basic,
        decimal fixedGross,
        decimal divisor,
        decimal expectedRounded)
    {
        var rate = PayrollCalculationRules.ResolvePerDayLopRate(lopBase, basic, fixedGross, divisor);
        Assert.Equal(expectedRounded, Math.Round(rate, 2));
    }

    [Fact]
    public void Caps_loss_of_pay_at_earned_fixed_gross_to_prevent_negative_net_pay()
    {
        // 31 unpaid days in a 31-day month when company policy uses FIXED_30_DAYS divisor:
        // Raw LOP = 31 * (90,000 / 30) = 93,000, which must be capped at 90,000 earned fixed gross.
        var perDayRate = PayrollCalculationRules.ResolvePerDayLopRate("FIXED_GROSS", 45000m, 90000m, 30m);
        var cappedLop = PayrollCalculationRules.CalculateCappedLossOfPay(31m, perDayRate, 90000m);

        Assert.Equal(90000m, cappedLop);
    }

    [Theory]
    [InlineData("EXEMPT", 40000, 15000, 0)]
    [InlineData("CAPPED_CEILING", 40000, 15000, 1800)]
    [InlineData("CAPPED_CEILING", 40000, 25000, 3000)]
    [InlineData("ACTUAL_BASIC_12", 40000, 15000, 4800)]
    public void Calculates_monthly_epf_by_company_mode(
        string epfMode,
        decimal earnedBasicAfterLop,
        decimal wageCeiling,
        decimal expectedEpf)
    {
        var epf = PayrollCalculationRules.CalculateMonthlyEpf(epfMode, earnedBasicAfterLop, wageCeiling);
        Assert.Equal(expectedEpf, epf);
    }

    [Theory]
    [InlineData("EXEMPT", 18000, 18000, 0)]
    [InlineData("STATUTORY", 18000, 18000, 135)]
    [InlineData("STATUTORY", 18500, 18500, 139)] // 18500 * 0.0075 = 138.75 -> ceiling to next rupee = 139
    [InlineData("STATUTORY", 25000, 25000, 0)]   // Above ₹21,000 threshold -> 0
    public void Calculates_statutory_esic_with_21k_threshold_and_rupee_ceiling(
        string esicMode,
        decimal contractualGross,
        decimal earnedGross,
        decimal expectedEsic)
    {
        var esic = PayrollCalculationRules.CalculateMonthlyEsic(esicMode, contractualGross, earnedGross);
        Assert.Equal(expectedEsic, esic);
    }

    [Theory]
    [InlineData(24000, 20000, 150)]
    [InlineData(24000, 25000, 0)]
    public void Calculates_esic_using_the_configured_wage_ceiling(decimal wageCeiling, decimal contractualGross, decimal expectedEsic)
    {
        var esic = PayrollCalculationRules.CalculateMonthlyEsic("STATUTORY", contractualGross, contractualGross, wageCeiling);
        Assert.Equal(expectedEsic, esic);
    }
    [Theory]
    [InlineData(1200000, "AUTO_NEW_REGIME", 0)]        // Gross 12.00L -> Taxable 11.25L -> ₹0 (Section 87A Rebate)
    [InlineData(1275000, "AUTO_NEW_REGIME", 0)]        // Gross 12.75L -> Taxable 12.00L -> ₹0 (Section 87A Rebate + ₹75k Std Ded)
    [InlineData(1285000, "AUTO_NEW_REGIME", 10400)]    // Gross 12.85L -> Taxable 12.10L -> Marginal Relief caps tax at ₹10k + 4% cess = ₹10,400
    [InlineData(1675000, "AUTO_NEW_REGIME", 124800)]   // Gross 16.75L -> Taxable 16.00L -> Slab tax ₹1.20L + 4% cess = ₹1,24,800
    [InlineData(1675000, "MANUAL_ONLY", 0)]            // Startup manual TDS mode -> ₹0 auto-deduction
    public void Calculates_fy26_section_115bac_annual_income_tax_with_standard_deduction_rebate_and_marginal_relief(
        decimal annualGrossIncome,
        string tdsMode,
        decimal expectedAnnualTax)
    {
        var annualTax = PayrollCalculationRules.CalculateAnnualIncomeTaxWithCess(annualGrossIncome, tdsMode);
        Assert.Equal(expectedAnnualTax, annualTax);
    }

    [Fact]
    public void Calculates_remaining_monthly_tds_using_ytd_tax_already_deducted()
    {
        // Annual tax on ₹16,75,000 is ₹1,24,800 (₹10,400/month).
        // If ₹62,400 was already deducted in the first 6 months (Apr-Sep), remaining 6 months (Oct-Mar) = ₹10,400/month.
        var monthlyTds = PayrollCalculationRules.CalculateMonthlyTds(
            annualGrossIncome: 1675000m,
            tdsMode: "AUTO_NEW_REGIME",
            ytdTaxAlreadyDeducted: 62400m,
            remainingMonthsInFy: 6);

        Assert.Equal(10400m, monthlyTds);
    }

    [Theory]
    [InlineData("A", 1)]
    [InlineData("a", 1)]
    [InlineData("HD", 0.5)]
    [InlineData("lhd", 0.5)]
    [InlineData("WFH-hd", 0.5)]
    [InlineData("P", 0)]
    public void Fallback_attendance_status_mapping_is_case_insensitive(string status, decimal expected) =>
        Assert.Equal(expected, PayrollCalculationRules.ResolveFallbackUnpaidDayFraction(status));
    [Theory]
    [InlineData(1.005, 1.01)]
    [InlineData(2.675, 2.68)]
    [InlineData(-1.005, -1.01)]
    public void Rounds_half_cent_money_values_away_from_zero(decimal value, decimal expected) =>
        Assert.Equal(expected, PayrollCalculationRules.RoundMoney(value));}
