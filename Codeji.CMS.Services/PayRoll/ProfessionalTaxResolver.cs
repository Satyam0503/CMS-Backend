namespace Codeji.CMS.Services.PayRoll;

public interface IProfessionalTaxResolver
{
    decimal CalculateMonthlyPt(
        string? stateOrJurisdiction,
        decimal monthlyEarnedGross,
        int payMonthNumber,
        string? gender = null,
        string? ptMode = "STATUTORY");

    string? ResolveEffectiveState(
        string? assignmentWorkLocationState,
        string? policyJurisdiction,
        string? companyState);
}

public sealed class ProfessionalTaxResolver : IProfessionalTaxResolver
{
    public decimal CalculateMonthlyPt(
        string? stateOrJurisdiction,
        decimal monthlyEarnedGross,
        int payMonthNumber,
        string? gender = null,
        string? ptMode = "STATUTORY")
    {
        return PayrollCalculationRules.CalculateMonthlyProfessionalTax(
            stateOrJurisdiction,
            monthlyEarnedGross,
            payMonthNumber,
            gender,
            ptMode);
    }

    public string? ResolveEffectiveState(
        string? assignmentWorkLocationState,
        string? policyJurisdiction,
        string? companyState)
    {
        if (!string.IsNullOrWhiteSpace(assignmentWorkLocationState))
            return assignmentWorkLocationState.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(policyJurisdiction))
            return policyJurisdiction.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(companyState))
            return companyState.Trim().ToUpperInvariant();

        return null;
    }
}
