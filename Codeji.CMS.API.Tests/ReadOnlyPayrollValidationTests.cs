using PayrollReadOnlyValidation;

namespace Codeji.CMS.API.Tests;

public sealed class ReadOnlyPayrollValidationTests
{
    [Theory]
    [InlineData("GET", "/api/payroll/configuration/salary-templates", true)]
    [InlineData("GET", "/api/payroll/configuration/policies", true)]
    [InlineData("POST", "/api/account/login", true)]
    [InlineData("POST", "/api/account/getSignedUserDetails", true)]
    [InlineData("POST", "/api/payroll/configuration/salary-templates", false)]
    [InlineData("POST", "/api/payroll/configuration/salary-templates/retire", false)]
    [InlineData("POST", "/api/payroll/configuration/policies/approve", false)]
    [InlineData("DELETE", "/api/payroll/configuration/policies/draft", false)]
    [InlineData("GET", "/api/AutoPayroll/GeneratePayRoll", false)]
    [InlineData("GET", "/api/account/verify-email", false)]
    [InlineData("GET", "/unknown", false)]
    [InlineData("PUT", "/api/account/login", false)]
    public void OnlyReviewedReadsAndApprovedSignInAreAllowed(string method, string path, bool expected)
        => Assert.Equal(expected, ReadOnlyRequestPolicy.IsAllowed(method, path));
}
