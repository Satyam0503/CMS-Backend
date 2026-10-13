using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeContractLifecycleRulesTests
{
    [Fact]
    public void Rejects_extension_of_ended_contract() =>
        Assert.Equal("CONTRACT_ALREADY_ENDED", EmployeeContractLifecycleRules.Validate("Extend", new EmployeeContractLifecycle { Status = "Ended" }, null, DateTime.UtcNow.Date.AddDays(10), false));

    [Fact]
    public void Rejects_extension_that_does_not_move_end_date_forward() =>
        Assert.Equal("CONTRACT_EXTENSION_DATE_INVALID", EmployeeContractLifecycleRules.Validate("Extend", new EmployeeContractLifecycle { Status = "Extended", EndDate = new DateTime(2026, 10, 10) }, null, new DateTime(2026, 10, 10), false));

    [Fact]
    public void Rejects_contract_end_before_start() =>
        Assert.Equal("CONTRACT_END_DATE_INVALID", EmployeeContractLifecycleRules.Validate("End", new EmployeeContractLifecycle { StartDate = new DateTime(2026, 10, 10) }, null, new DateTime(2026, 10, 9), false));

    [Fact]
    public void Rejects_contract_end_when_an_exit_is_active() =>
        Assert.Equal("ACTIVE_EXIT_ALREADY_EXISTS", EmployeeContractLifecycleRules.Validate("End", new EmployeeContractLifecycle(), null, null, true));

    [Fact]
    public void Accepts_valid_extension_and_end() {
        Assert.Null(EmployeeContractLifecycleRules.Validate("Extend", new EmployeeContractLifecycle { Status = "FixedTerm", EndDate = new DateTime(2026, 10, 10) }, null, new DateTime(2026, 11, 10), false));
        Assert.Null(EmployeeContractLifecycleRules.Validate("End", new EmployeeContractLifecycle { StartDate = new DateTime(2026, 1, 1) }, null, new DateTime(2026, 10, 10), false));
    }
}
