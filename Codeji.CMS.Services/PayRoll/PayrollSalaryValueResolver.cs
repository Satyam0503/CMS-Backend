namespace Codeji.CMS.Services.PayRoll;

public static class PayrollSalaryValueResolver
{
    public static decimal Resolve(bool hasApprovedEmployeeStructure, decimal? templateValue, decimal employeeStructureValue, decimal legacyValue) =>
        hasApprovedEmployeeStructure ? employeeStructureValue : templateValue ?? legacyValue;
}
