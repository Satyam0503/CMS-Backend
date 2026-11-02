using Codeji.CMS.Repository.Entities.Employees;
public class PayrollCalculationRulesTests
{
    [Fact]
    public void Automatic_tds_rejects_non_taxable_earning_without_rule_specific_exemption_inputs()
    {
        var issue = PayrollCalculationRules.FindUnsupportedAutomaticTdsTreatment(
            [new PayrollComponentSnapshot { Code = "MEAL", Name = "Meal", Kind = "EARNING", IsTaxable = false }],
            "AUTO_NEW_REGIME");

        Assert.Contains("exemption category, eligibility, or evidence", issue);
        Assert.Contains("MEAL", issue);
    }

    [Fact]
    public void Automatic_tds_accepts_taxable_earnings_and_manual_mode_does_not_project_them()
    {
        var taxable = new[] { new PayrollComponentSnapshot { Code = "BASIC", Name = "Basic", Kind = "EARNING", IsTaxable = true } };
        var nonTaxableDeduction = new[] { new PayrollComponentSnapshot { Code = "LOAN", Name = "Loan recovery", Kind = "DEDUCTION", IsTaxable = false } };

        Assert.Null(PayrollCalculationRules.FindUnsupportedAutomaticTdsTreatment(taxable, "AUTO_NEW_REGIME"));
        Assert.Null(PayrollCalculationRules.FindUnsupportedAutomaticTdsTreatment(nonTaxableDeduction, "AUTO_NEW_REGIME"));
        Assert.Null(PayrollCalculationRules.FindUnsupportedAutomaticTdsTreatment(
            [new PayrollComponentSnapshot { Code = "MEAL", Name = "Meal", Kind = "EARNING", IsTaxable = false }], "MANUAL_ONLY"));
    }

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
    [InlineData("CAPPED_CEILING", 15000, 10, 1500)]
    [InlineData("ACTUAL_BASIC_12", 12345.67, 10.5, 1296.30)]
    public void Calculates_epf_from_configured_rate_without_intermediate_rounding(
        string epfMode,
        decimal earnedBasicAfterLop,
        decimal ratePercent,
        decimal expectedEpf)
    {
        var epf = PayrollCalculationRules.CalculateMonthlyEpf(epfMode, earnedBasicAfterLop, 15000m, ratePercent);
        Assert.Equal(expectedEpf, epf);
    }

    [Fact]
    public void Uses_configured_esic_employee_rate()
    {
        var esic = PayrollCalculationRules.CalculateMonthlyEsic("STATUTORY", 20000m, 20000m, 21000m, 1m);
        Assert.Equal(200m, esic);
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

    [Fact]
    public void Calculates_statutory_hra_exemption_metro_vs_non_metro()
    {
        // Basic: 6,00,000, HRA Received: 2,40,000, Rent Paid: 1,80,000
        // 1. Actual HRA = 2,40,000
        // 2. Rent - 10% of basic = 1,80,000 - 60,000 = 1,20,000
        // 3. 50% of Basic (Metro) = 3,00,000; 40% (Non-Metro) = 2,40,000
        // Metro Exemption = Min(240k, 120k, 300k) = 1,20,000
        // Non-Metro Exemption = Min(240k, 120k, 240k) = 1,20,000
        var metroExemption = PayrollCalculationRules.CalculateStatutoryHraExemption(600000m, 240000m, 180000m, isMetro: true);
        var nonMetroExemption = PayrollCalculationRules.CalculateStatutoryHraExemption(600000m, 240000m, 180000m, isMetro: false);

        Assert.Equal(120000m, metroExemption);
        Assert.Equal(120000m, nonMetroExemption);

        // When rent paid is high (3,60,000):
        // Rent - 10% basic = 3,60,000 - 60,000 = 3,00,000
        // Metro (50%) = 3,00,000 -> Min(240k, 300k, 300k) = 2,40,000 (limited by actual HRA)
        var highRentExemption = PayrollCalculationRules.CalculateStatutoryHraExemption(600000m, 240000m, 360000m, isMetro: true);
        Assert.Equal(240000m, highRentExemption);
    }

    [Fact]
    public void Calculates_old_regime_tax_with_statutory_deductions_and_surcharge()
    {
        // Gross: 15,00,000
        // Deductions: Std (50k) + 80C (1.5L) + 80D (25k) + Section 24 (2L) = 4,25,000
        // Taxable: 15L - 4.25L = 10,75,000
        // Slabs:
        // 0 to 2.5L: 0
        // 2.5L to 5L: 12,500
        // 5L to 10L: 1,00,000
        // 10L to 10.75L: 75,000 * 30% = 22,500
        // Tax before cess = 1,35,000
        // 4% cess = 5,400
        // Total Tax = 1,40,400
        var tax = PayrollCalculationRules.CalculateAnnualIncomeTaxWithCess(
            annualGrossIncome: 1500000m,
            tdsMode: "OLD_REGIME",
            previousEmployerGrossSalary: 0m,
            otherTaxableIncome: 0m,
            approved80C: 150000m,
            approved80Ccd1B: 0m,
            approved80D: 25000m,
            approvedSection24Loss: 200000m);

        Assert.Equal(140400m, tax);
    }

    [Theory]
    [InlineData("AUTO_NEW_REGIME")]
    [InlineData("auto_old_regime")]
    [InlineData("NEW_115BAC")]
    [InlineData("old_regime")]
    [InlineData("MANUAL_ONLY")]
    [InlineData(null)]
    [InlineData("")]
    public void Accepts_only_supported_tds_modes_with_legacy_blank_default(string? tdsMode) =>
        Assert.True(PayrollCalculationRules.IsSupportedTdsMode(tdsMode));

    [Fact]
    public void Rejects_unknown_tds_mode_instead_of_silently_using_new_regime() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PayrollCalculationRules.CalculateAnnualIncomeTaxWithCess(1675000m, "AUTO_NEW_REGIMEE"));

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
        Assert.Equal(expected, PayrollCalculationRules.RoundMoney(value));

    [Theory]
    [InlineData("CAPPED_CEILING", 30000, 550.50, 1249.50, 75.00, 75.00)]
    [InlineData("CAPPED_CEILING", 15000, 550.50, 1249.50, 75.00, 75.00)]
    [InlineData("CAPPED_CEILING", 10000, 367.00, 833.00, 50.00, 50.00)]
    [InlineData("EXEMPT", 30000, 0, 0, 0, 0)]
    public void Calculates_employer_monthly_epf_breakdown(
        string epfMode,
        decimal basic,
        decimal expectedEpf,
        decimal expectedEps,
        decimal expectedEdli,
        decimal expectedAdmin)
    {
        var (epf, eps, edli, admin) = PayrollCalculationRules.CalculateEmployerMonthlyEpf(epfMode, basic);
        Assert.Equal(expectedEpf, epf);
        Assert.Equal(expectedEps, eps);
        Assert.Equal(expectedEdli, edli);
        Assert.Equal(expectedAdmin, admin);
    }

    [Fact]
    public void CalculateGratuity_ReturnsZero_WhenTenureLessThanFiveYears()
    {
        var doj = new DateTime(2022, 1, 1);
        var lwd = new DateTime(2025, 1, 1); // 3 years
        var gratuity = PayrollCalculationRules.CalculateGratuity(50000m, doj, lwd);
        Assert.Equal(0m, gratuity);
    }

    [Fact]
    public void CalculateGratuity_CalculatesCorrectStatutoryAmount_WhenTenureExceedsFiveYears()
    {
        var doj = new DateTime(2020, 1, 1);
        var lwd = new DateTime(2026, 1, 1); // 6 years
        // (15 * 50000 * 6) / 26 = 4500000 / 26 = 173076.92
        var gratuity = PayrollCalculationRules.CalculateGratuity(50000m, doj, lwd);
        Assert.Equal(173076.92m, gratuity);
    }

    [Fact]
    public void CalculateGratuity_EnforcesStatutoryCeilingOfTwentyLakhs()
    {
        var doj = new DateTime(2000, 1, 1);
        var lwd = new DateTime(2026, 1, 1); // 26 years
        // (15 * 500000 * 26) / 26 = 7500000 -> capped at 2000000
        var gratuity = PayrollCalculationRules.CalculateGratuity(500000m, doj, lwd);
        Assert.Equal(2000000m, gratuity);
    }

    [Fact]
    public void CalculateLeaveEncashment_CalculatesProportionalBasicPayout()
    {
        // (60000 / 30) * 12 = 24000
        var encashment = PayrollCalculationRules.CalculateLeaveEncashment(60000m, 12m, 30m);
        Assert.Equal(24000m, encashment);
    }

    [Fact]
    public void CalculateNoticeShortfallRecovery_CalculatesCorrectRecoveryAmount()
    {
        // 60 required, 45 served -> 15 shortfall days -> (60000 / 30) * 15 = 30000
        var recovery = PayrollCalculationRules.CalculateNoticeShortfallRecovery(60, 45, 60000m, 30m);
        Assert.Equal(30000m, recovery);
    }
}
