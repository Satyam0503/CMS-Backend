using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollReadinessServiceTests
{
    private readonly Mock<IMongoDbRepository<Company>> _companies = new();
    private readonly Mock<IMongoDbRepository<PayrollPolicyProfile>> _policies = new();
    private readonly Mock<IMongoDbRepository<SalaryTemplate>> _templates = new();
    private readonly Mock<IMongoDbRepository<PayrollPayGroup>> _payGroups = new();
    private readonly Mock<IMongoDbRepository<PayrollSalaryComponentDefinition>> _components = new();
    private readonly Mock<IMongoDbRepository<EmployeePayrollAssignment>> _assignments = new();
    private readonly Mock<IMongoDbRepository<EmployeeSalaryStructure>> _salaryStructures = new();
    private readonly Mock<IMongoDbRepository<EmpUser>> _employees = new();
    private readonly Mock<IMongoDbRepository<MonthlyAttendanceSummary>> _attendance = new();

    private PayrollReadinessService CreateService() => new(
        _companies.Object,
        _policies.Object,
        _templates.Object,
        _payGroups.Object,
        _components.Object,
        _assignments.Object,
        _salaryStructures.Object,
        _employees.Object,
        _attendance.Object);

    [Fact]
    public async Task Validate_ReturnsBlocked_WhenCompanyDoesNotExist()
    {
        _companies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Company?)null);

        var service = CreateService();
        var report = await service.ValidateCompanyPayrollReadiness("company-1", new DateTime(2026, 4, 1));

        Assert.False(report.IsReady);
        Assert.Contains(report.BlockingIssues, msg => msg.Contains("Company entity does not exist"));
        Assert.Equal("BLOCKED", report.Steps.First(s => s.StepKey == "COMPANY_PROFILE").Status);
    }

    [Fact]
    public async Task Validate_ReturnsBlocked_WhenCompanyHasNoRegisteredState()
    {
        _companies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Acme Corp", State = "", DefaultLanguage = "en" });

        _policies.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPolicyProfile>());

        var service = CreateService();
        var report = await service.ValidateCompanyPayrollReadiness("company-1", new DateTime(2026, 4, 1));

        Assert.False(report.IsReady);
        Assert.Contains(report.BlockingIssues, msg => msg.Contains("no registered State"));
    }

    [Fact]
    public async Task Validate_ReturnsBlocked_WhenNoApprovedActivePolicyCoversPeriod()
    {
        _companies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Acme Corp", State = "MH", DefaultLanguage = "en" });

        _policies.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPolicyProfile>
            {
                new() { CompanyId = "company-1", Name = "Draft Policy", ApprovalStatus = "DRAFT", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>());

        _templates.Setup(x => x.GetAll(It.IsAny<Expression<Func<SalaryTemplate, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SalaryTemplate>());

        _payGroups.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPayGroup, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPayGroup>());

        var service = CreateService();
        var report = await service.ValidateCompanyPayrollReadiness("company-1", new DateTime(2026, 4, 1));

        Assert.False(report.IsReady);
        Assert.Contains(report.BlockingIssues, msg => msg.Contains("No approved, active payroll policy profile"));
    }

    [Fact]
    public async Task Validate_ReturnsBlocked_WhenNoActiveComponentsOrTemplates()
    {
        _companies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Acme Corp", State = "MH", DefaultLanguage = "en" });

        _policies.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPolicyProfile>
            {
                new()
                {
                    CompanyId = "company-1",
                    Name = "Approved Policy",
                    ApprovalStatus = "APPROVED",
                    IsActive = true,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    StatutoryJurisdiction = "IN-MH",
                    FinancialYear = "2026-27",
                    StatutoryRuleVersion = "2026.1"
                }
            });

        _components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>());

        _templates.Setup(x => x.GetAll(It.IsAny<Expression<Func<SalaryTemplate, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SalaryTemplate>());

        _payGroups.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPayGroup, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPayGroup>());

        var service = CreateService();
        var report = await service.ValidateCompanyPayrollReadiness("company-1", new DateTime(2026, 4, 1));

        Assert.False(report.IsReady);
        Assert.Contains(report.BlockingIssues, msg => msg.Contains("No active salary components configured"));
        Assert.Contains(report.BlockingIssues, msg => msg.Contains("No active salary templates configured"));
    }

    [Fact]
    public async Task Validate_ReturnsBlocked_WhenEmployeesLackApprovedSalaryStructures()
    {
        _companies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Acme Corp", State = "MH", DefaultLanguage = "en" });

        _policies.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPolicyProfile>
            {
                new()
                {
                    CompanyId = "company-1",
                    Name = "Approved Policy",
                    ApprovalStatus = "APPROVED",
                    IsActive = true,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    StatutoryJurisdiction = "IN-MH",
                    FinancialYear = "2026-27",
                    StatutoryRuleVersion = "2026.1"
                }
            });

        _components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>
            {
                new() { CompanyId = "company-1", Code = "BASIC", Name = "Basic", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _templates.Setup(x => x.GetAll(It.IsAny<Expression<Func<SalaryTemplate, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SalaryTemplate>
            {
                new() { CompanyId = "company-1", Name = "Standard", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _payGroups.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPayGroup, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPayGroup>
            {
                new() { CompanyId = "company-1", Name = "Monthly", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new() { CompanyId = "company-1", EmployeeId = "EMP-001", Status = true, FirstName = "John", LastName = "Doe", Email = "john@example.com" }
            });

        _assignments.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeePayrollAssignment, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmployeePayrollAssignment>());

        _salaryStructures.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeeSalaryStructure, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmployeeSalaryStructure>());

        var service = CreateService();
        var report = await service.ValidateCompanyPayrollReadiness("company-1", new DateTime(2026, 4, 1));

        Assert.False(report.IsReady);
        Assert.Contains(report.BlockingIssues, msg => msg.Contains("None of the 1 active employees have both an approved payroll assignment and approved salary structure"));
    }

    [Fact]
    public async Task Validate_ReturnsBlocked_WhenAttendanceLockRequired_AndNotLocked()
    {
        _companies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Acme Corp", State = "MH", DefaultLanguage = "en" });

        _policies.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPolicyProfile>
            {
                new()
                {
                    CompanyId = "company-1",
                    Name = "Approved Policy",
                    ApprovalStatus = "APPROVED",
                    IsActive = true,
                    RequireAttendanceLock = true,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    StatutoryJurisdiction = "IN-MH",
                    FinancialYear = "2026-27",
                    StatutoryRuleVersion = "2026.1"
                }
            });

        _components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>
            {
                new() { CompanyId = "company-1", Code = "BASIC", Name = "Basic", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _templates.Setup(x => x.GetAll(It.IsAny<Expression<Func<SalaryTemplate, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SalaryTemplate>
            {
                new() { CompanyId = "company-1", Name = "Standard", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _payGroups.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPayGroup, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPayGroup>
            {
                new() { CompanyId = "company-1", Name = "Monthly", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new() { CompanyId = "company-1", EmployeeId = "EMP-001", Status = true, FirstName = "John", LastName = "Doe", Email = "john@example.com" }
            });

        _assignments.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeePayrollAssignment, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmployeePayrollAssignment>
            {
                new() { CompanyId = "company-1", EmployeeId = "EMP-001", IsApproved = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _salaryStructures.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeeSalaryStructure, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmployeeSalaryStructure>
            {
                new() { CompanyId = "company-1", EmployeeId = "EMP-001", ApprovalStatus = "APPROVED", EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _attendance.Setup(x => x.GetAll(It.IsAny<Expression<Func<MonthlyAttendanceSummary, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<MonthlyAttendanceSummary>
            {
                new() { EmployeeId = "EMP-001", UserId = "U-1", PayrollMonth = new DateTime(2026, 4, 1), IsLocked = false, IsApproved = false }
            });

        var service = CreateService();
        var report = await service.ValidateCompanyPayrollReadiness("company-1", new DateTime(2026, 4, 1));

        Assert.False(report.IsReady);
        Assert.False(report.IsAttendanceLocked);
        Assert.Contains(report.BlockingIssues, msg => msg.Contains("Monthly attendance for 1 employee(s) is not approved and locked"));
    }

    [Fact]
    public async Task Validate_ReturnsReady_WhenAllRequirementsMet()
    {
        _companies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Acme Corp", State = "MH", DefaultLanguage = "en" });

        _policies.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPolicyProfile>
            {
                new()
                {
                    CompanyId = "company-1",
                    Name = "Approved Policy",
                    ApprovalStatus = "APPROVED",
                    IsActive = true,
                    RequireAttendanceLock = true,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    StatutoryJurisdiction = "IN-MH",
                    FinancialYear = "2026-27",
                    StatutoryRuleVersion = "2026.1"
                }
            });

        _components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>
            {
                new() { CompanyId = "company-1", Code = "BASIC", Name = "Basic", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _templates.Setup(x => x.GetAll(It.IsAny<Expression<Func<SalaryTemplate, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SalaryTemplate>
            {
                new() { CompanyId = "company-1", Name = "Standard", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _payGroups.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollPayGroup, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollPayGroup>
            {
                new() { CompanyId = "company-1", Name = "Monthly", IsActive = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new() { CompanyId = "company-1", EmployeeId = "EMP-001", Status = true, FirstName = "John", LastName = "Doe", Email = "john@example.com", BankAccountNumber = 1234567890, PanNumber = "ABCDE1234F" }
            });

        _assignments.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeePayrollAssignment, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmployeePayrollAssignment>
            {
                new() { CompanyId = "company-1", EmployeeId = "EMP-001", IsApproved = true, EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _salaryStructures.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeeSalaryStructure, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmployeeSalaryStructure>
            {
                new() { CompanyId = "company-1", EmployeeId = "EMP-001", ApprovalStatus = "APPROVED", EffectiveFrom = new DateTime(2026, 1, 1) }
            });

        _attendance.Setup(x => x.GetAll(It.IsAny<Expression<Func<MonthlyAttendanceSummary, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<MonthlyAttendanceSummary>
            {
                new() { EmployeeId = "EMP-001", UserId = "U-1", PayrollMonth = new DateTime(2026, 4, 1), IsLocked = true, IsApproved = true }
            });

        var service = CreateService();
        var report = await service.ValidateCompanyPayrollReadiness("company-1", new DateTime(2026, 4, 1));

        Assert.True(report.IsReady);
        Assert.Empty(report.BlockingIssues);
        Assert.True(report.IsAttendanceLocked);
        Assert.Equal(1, report.ConfiguredEmployeesCount);
        Assert.All(report.Steps, step => Assert.NotEqual("BLOCKED", step.Status));
    }
}
