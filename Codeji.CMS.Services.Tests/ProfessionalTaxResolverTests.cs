using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class ProfessionalTaxResolverTests
{
    private readonly IProfessionalTaxResolver _resolver = new ProfessionalTaxResolver();

    [Theory]
    [InlineData("MH", 6000, 1, "Male", 0)]
    [InlineData("MH", 8500, 1, "Male", 175)]
    [InlineData("MH", 15000, 1, "Male", 200)]
    [InlineData("MH", 15000, 2, "Male", 300)] // February ₹300
    [InlineData("IN-MH", 40000, 3, "Male", 200)]
    [InlineData("Maharashtra", 40000, 2, "Male", 300)]
    public void Calculates_maharashtra_pt_for_men_with_february_surge(
        string jurisdiction, decimal gross, int month, string gender, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, month, gender);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("MH", 15000, 1, "Female", 0)] // Exempt up to ₹25,000 for women
    [InlineData("MH", 25000, 1, "Female", 0)] // Exactly 25,000 is exempt
    [InlineData("MH", 30000, 1, "Female", 200)]
    [InlineData("MH", 30000, 2, "Female", 300)] // February surge for women > 25k
    [InlineData("IN-MH", 18000, 2, "WOMAN", 0)]
    public void Calculates_maharashtra_pt_with_women_statutory_exemption(
        string jurisdiction, decimal gross, int month, string gender, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, month, gender);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("KA", 15000, 0)]
    [InlineData("KA", 25000, 0)] // <= 25,000 is Nil under amended Karnataka PT Act
    [InlineData("KA", 25001, 200)]
    [InlineData("IN-KA", 60000, 200)]
    [InlineData("Karnataka", 120000, 200)]
    public void Calculates_karnataka_pt_with_25k_nil_threshold(
        string jurisdiction, decimal gross, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, 1);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("TG", 12000, 0)]
    [InlineData("TG", 18000, 150)]
    [InlineData("TG", 30000, 200)]
    [InlineData("IN-TS", 25000, 200)]
    [InlineData("AP", 12000, 0)]
    [InlineData("AP", 17000, 150)]
    [InlineData("Andhra Pradesh", 45000, 200)]
    public void Calculates_telangana_and_andhra_pradesh_pt_slabs(
        string jurisdiction, decimal gross, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, 1);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("GJ", 10000, 0)]
    [InlineData("GJ", 12000, 0)]
    [InlineData("GJ", 15000, 200)]
    [InlineData("IN-GJ", 50000, 200)]
    public void Calculates_gujarat_pt_slabs(
        string jurisdiction, decimal gross, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, 1);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("WB", 8000, 0)]
    [InlineData("WB", 12000, 110)]
    [InlineData("WB", 20000, 130)]
    [InlineData("WB", 30000, 150)]
    [InlineData("WB", 45000, 200)]
    [InlineData("IN-WB", 80000, 200)]
    public void Calculates_west_bengal_pt_slabs(
        string jurisdiction, decimal gross, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, 1);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("TN", 15000, 0)]
    [InlineData("TN", 25000, 100)]
    [InlineData("TN", 40000, 235)]
    [InlineData("TN", 55000, 510)]
    [InlineData("TN", 70000, 760)]
    [InlineData("TN", 90000, 1025)]
    public void Calculates_tamil_nadu_pt_slabs(
        string jurisdiction, decimal gross, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, 1);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("MP", 15000, 1, 0)]
    [InlineData("MP", 22000, 1, 125)]
    [InlineData("MP", 22000, 3, 135)] // March adjustment
    [InlineData("MP", 30000, 1, 166)]
    [InlineData("MP", 30000, 3, 174)] // March adjustment
    [InlineData("MP", 50000, 1, 208)]
    [InlineData("MP", 50000, 3, 212)] // March adjustment
    public void Calculates_madhya_pradesh_pt_with_march_adjustments(
        string jurisdiction, decimal gross, int month, decimal expected)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, month);
        Assert.Equal(expected, pt);
    }

    [Theory]
    [InlineData("DL", 80000)]
    [InlineData("HR", 90000)]
    [InlineData("UP", 75000)]
    [InlineData("RJ", 65000)]
    [InlineData("", 50000)]
    [InlineData(null, 50000)]
    public void Returns_zero_pt_for_non_pt_states(string? jurisdiction, decimal gross)
    {
        var pt = _resolver.CalculateMonthlyPt(jurisdiction, gross, 1);
        Assert.Equal(0m, pt);
    }

    [Fact]
    public void Returns_zero_when_pt_mode_is_exempt_or_disabled()
    {
        Assert.Equal(0m, _resolver.CalculateMonthlyPt("MH", 50000, 1, ptMode: "EXEMPT"));
        Assert.Equal(0m, _resolver.CalculateMonthlyPt("KA", 50000, 1, ptMode: "DISABLED"));
    }

    [Fact]
    public void Resolves_effective_state_with_proper_precedence()
    {
        // 1. Assignment override wins over policy and company
        Assert.Equal("MH", _resolver.ResolveEffectiveState("MH", "KA", "DL"));

        // 2. Policy jurisdiction wins when assignment is omitted
        Assert.Equal("KA", _resolver.ResolveEffectiveState(null, "KA", "DL"));
        Assert.Equal("KA", _resolver.ResolveEffectiveState(" ", "KA", "DL"));

        // 3. Company state is fallback when assignment and policy are omitted
        Assert.Equal("DL", _resolver.ResolveEffectiveState(null, null, "DL"));

        // 4. Returns null when all are missing
        Assert.Null(_resolver.ResolveEffectiveState(null, null, null));
    }
}
