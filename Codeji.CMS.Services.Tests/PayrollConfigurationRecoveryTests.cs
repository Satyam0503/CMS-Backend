using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollConfigurationRecoveryTests
{
    private const string Company = "company-1";
    private static readonly DateTime Start = new(2026, 10, 1);

    [Fact]
    public async Task GroupReactivationUpdatesTheSameTenantRecordAndIncrementsVersion()
    {
        var groups = Repository(new PayrollPayGroup { Id = "group-1", CompanyId = Company, Name = "Monthly", IsActive = false, Version = 4, EffectiveFrom = Start });
        var service = new PayrollConfigurationService(groups.Object, Repository<EmployeePayrollAssignment>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPolicyProfile>().Object);
        var result = await service.SavePayGroup(Company, "admin", new PayrollPayGroupDto { Id = "group-1", Name = "Monthly", EffectiveFrom = Start, IsActive = true, Version = 4 });
        Assert.True(result.Success);
        groups.Verify(repository => repository.Update(It.IsAny<FilterDefinition<PayrollPayGroup>>(), It.Is<PayrollPayGroup>(value => value.Id == "group-1" && value.CompanyId == Company && value.IsActive && value.Version == 5)), Times.Once);
        groups.Verify(repository => repository.AddOne(It.IsAny<PayrollPayGroup>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task GroupUpdateRejectsMissingOrStaleVersion(int version)
    {
        var groups = Repository(new PayrollPayGroup { Id = "group-1", CompanyId = Company, Name = "Monthly", Version = 4, EffectiveFrom = Start });
        var service = new PayrollConfigurationService(groups.Object, Repository<EmployeePayrollAssignment>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPolicyProfile>().Object);
        var result = await service.SavePayGroup(Company, "admin", new PayrollPayGroupDto { Id = "group-1", Name = "Monthly", EffectiveFrom = Start, Version = version });
        Assert.False(result.Success); Assert.Equal(409, result.StatusCode);
        groups.Verify(repository => repository.Update(It.IsAny<FilterDefinition<PayrollPayGroup>>(), It.IsAny<PayrollPayGroup>()), Times.Never);
    }

    [Fact]
    public async Task GroupUpdateCannotFindAnotherTenantsRecord()
    {
        var groups = Repository(new PayrollPayGroup { Id = "group-1", CompanyId = "another-company", Name = "Monthly", Version = 4, EffectiveFrom = Start });
        var service = new PayrollConfigurationService(groups.Object, Repository<EmployeePayrollAssignment>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPolicyProfile>().Object);
        var result = await service.SavePayGroup(Company, "admin", new PayrollPayGroupDto { Id = "group-1", Name = "Monthly", EffectiveFrom = Start, Version = 4 });
        Assert.False(result.Success); Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ComponentCannotBeRevisedFromSupersededVersion()
    {
        var definitions = Repository(Definition("meal-v1", 1), Definition("meal-v2", 2));
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object, definitions.Object);
        var result = await service.SaveComponent(Company, "admin", new PayrollSalaryComponentDto { Id = "meal-v1", Version = 1, Code = "MEAL", Name = "Meal", EffectiveFrom = Start });
        Assert.False(result.Success); Assert.Equal(409, result.StatusCode);
        definitions.Verify(repository => repository.AddOne(It.IsAny<PayrollSalaryComponentDefinition>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task TemplateUpdateRejectsMissingOrStaleVersion(int version)
    {
        var templates = Repository(new SalaryTemplate { Id = "template-1", CompanyId = Company, Name = "Junior", EffectiveFrom = Start, Version = 4 });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto
        {
            Id = "template-1", Version = version, Name = "Junior revised", EffectiveFrom = Start, BasicPay = 30000
        });
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        templates.Verify(repository => repository.Update(It.IsAny<FilterDefinition<SalaryTemplate>>(), It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Fact]
    public async Task TemplateUpdateCannotRewriteSavedSalaryHistory()
    {
        var templates = Repository(new SalaryTemplate { Id = "template-1", CompanyId = Company, Name = "Junior", EffectiveFrom = Start, Version = 4 });
        templates.Setup(repository => repository.Update(It.IsAny<FilterDefinition<SalaryTemplate>>(), It.IsAny<SalaryTemplate>()))
            .ReturnsAsync(new Result { Success = false, StatusCode = 400, Message = "No matching template version." });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto
        {
            Id = "template-1", Version = 4, Name = "Junior revised", EffectiveFrom = Start, BasicPay = 30000
        });
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("immutable", result.Message, StringComparison.OrdinalIgnoreCase);
        templates.Verify(repository => repository.Update(It.IsAny<FilterDefinition<SalaryTemplate>>(), It.IsAny<SalaryTemplate>()), Times.Never);
        templates.Verify(repository => repository.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Fact]
    public async Task TemplateRevisionCreatesIndependentRecordAndPreservesSource()
    {
        var original = new SalaryTemplate { Id = "original", CompanyId = Company, Name = "Junior", EffectiveFrom = Start, Version = 4, BasicPay = 20000 };
        var templates = Repository(original);
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto {
            SourceTemplateId = original.Id, SourceTemplateVersion = 4, Version = 999, Name = "Junior revised", EffectiveFrom = Start.AddMonths(1), BasicPay = 30000
        });
        Assert.True(result.Success);
        Assert.Equal(20000m, original.BasicPay);
        templates.Verify(repository => repository.AddOne(It.Is<SalaryTemplate>(value => value.Id == "" && value.Version == 1 && value.SourceTemplateId == "original" && value.SourceTemplateVersion == 4 && value.BasicPay == 30000)), Times.Once);
        templates.Verify(repository => repository.Update(It.IsAny<FilterDefinition<SalaryTemplate>>(), It.IsAny<SalaryTemplate>()), Times.Never);
        var listed = await service.GetTemplates(Company);
        Assert.Equal(4, listed.Single().Version);
    }

    [Theory]
    [InlineData("another-company", 4, 404)]
    [InlineData(Company, 3, 409)]
    [InlineData(Company, null, 409)]
    public async Task TemplateRevisionRejectsForeignOrStaleSource(string sourceCompany, int? sourceVersion, int expectedStatus)
    {
        var templates = Repository(new SalaryTemplate { Id = "original", CompanyId = sourceCompany, Name = "Junior", EffectiveFrom = Start, Version = 4 });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto {
            SourceTemplateId = "original", SourceTemplateVersion = sourceVersion, Name = "Revision", EffectiveFrom = Start
        });
        Assert.False(result.Success); Assert.Equal(expectedStatus, result.StatusCode);
        templates.Verify(repository => repository.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Fact]
    public async Task TemplateRevisionCannotSupplyVersionWithoutSource()
    {
        var templates = Repository<SalaryTemplate>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto { SourceTemplateVersion = 2, Name = "Invalid", EffectiveFrom = Start });
        Assert.False(result.Success); Assert.Equal(400, result.StatusCode);
        templates.Verify(repository => repository.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Fact]
    public async Task TemplateCannotBypassDuplicateCodesBySpoofingPayloadCodes()
    {
        var definitions = Repository(Definition("meal-v1", 1), Definition("meal-v2", 2));
        var templates = Repository<SalaryTemplate>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object, definitions.Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto { Name = "Junior", EffectiveFrom = Start,
            Components = [new() { ComponentId = "meal-v1", Code = "MEAL", Name = "Meal", Value = 100 }, new() { ComponentId = "meal-v2", Code = "DIFFERENT", Name = "Different", Value = 100 }] });
        Assert.False(result.Success); Assert.Equal(409, result.StatusCode);
        templates.Verify(repository => repository.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Theory]
    [InlineData(false, false, 404)]
    [InlineData(true, true, 400)]
    public async Task TemplateRejectsInactiveOrInsufficientGroupWindow(bool active, bool finite, int status)
    {
        var groups = Repository(new PayrollPayGroup { Id = "group-1", CompanyId = Company, Name = "Monthly", IsActive = active, EffectiveFrom = Start, EffectiveTo = finite ? Start.AddMonths(1).AddDays(-1) : null });
        var templates = Repository<SalaryTemplate>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, groups.Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto { Name = "Junior", PayGroupId = "group-1", EffectiveFrom = Start, BasicPay = 30000 });
        Assert.False(result.Success); Assert.Equal(status, result.StatusCode);
        templates.Verify(repository => repository.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Fact]
    public async Task TemplateSavesTheLatestEligibleComponentOnce()
    {
        var definitions = Repository(Definition("meal-v1", 1), Definition("meal-v2", 2));
        var templates = Repository<SalaryTemplate>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object, definitions.Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto { Name = "Junior", EffectiveFrom = Start, BasicPay = 30000,
            Components = [new() { ComponentId = "meal-v2", Code = "MEAL", Name = "Meal", Value = 1000 }] });
        Assert.True(result.Success);
        templates.Verify(repository => repository.AddOne(It.Is<SalaryTemplate>(value => value.Components.Count == 1 && value.Components[0].ComponentId == "meal-v2" && value.Components[0].Value == 1000)), Times.Once);
    }

    [Fact]
    public async Task TemplateRejectsNegativeAmounts()
    {
        var templates = Repository<SalaryTemplate>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto { Name = "Invalid", EffectiveFrom = Start, BasicPay = -1 });
        Assert.False(result.Success); Assert.Equal(400, result.StatusCode);
        templates.Verify(repository => repository.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    private static PayrollSalaryComponentDefinition Definition(string id, int version) =>
        new() { Id = id, CompanyId = Company, Code = "MEAL", Name = "Meal", Kind = "EARNING", CalculationType = "FIXED", EffectiveFrom = Start, IsActive = true, Version = version };

    [Fact]
    public async Task TemplateRetirementPreservesContentAndExistingPayrollAvailability()
    {
        var templates = Repository(new SalaryTemplate { Id = "template", CompanyId = Company, Name = "Engineer", EffectiveFrom = Start, BasicPay = 30000, Version = 2 });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.RetireTemplate(Company, "admin", "template", 2);
        Assert.True(result.Success);
        templates.Verify(repository => repository.Update(It.IsAny<FilterDefinition<SalaryTemplate>>(), It.Is<SalaryTemplate>(value => value.IsActive && value.IsRetired && value.Version == 3 && value.BasicPay == 30000 && value.RetiredBy == "admin" && value.RetiredAtUtc.HasValue)), Times.Once);
        var listed = (await service.GetTemplates(Company)).Single();
        Assert.True(listed.IsRetired); Assert.False(listed.IsActive);
    }

    [Theory]
    [InlineData("another-company", 2, 404)]
    [InlineData(Company, 1, 409)]
    public async Task RetirementRejectsForeignTenantAndStaleVersion(string owner, int version, int status)
    {
        var templates = Repository(new SalaryTemplate { Id = "template", CompanyId = owner, Name = "Engineer", EffectiveFrom = Start, Version = 2 });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        var result = await service.RetireTemplate(Company, "admin", "template", version);
        Assert.False(result.Success); Assert.Equal(status, result.StatusCode);
        templates.Verify(repository => repository.Update(It.IsAny<FilterDefinition<SalaryTemplate>>(), It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Fact]
    public async Task RetirementReportsConcurrentUpdate()
    {
        var templates = Repository(new SalaryTemplate { Id = "template", CompanyId = Company, Name = "Engineer", EffectiveFrom = Start, Version = 2 });
        templates.Setup(repository => repository.Update(It.IsAny<FilterDefinition<SalaryTemplate>>(), It.IsAny<SalaryTemplate>())).ReturnsAsync(new Result { Success = false, StatusCode = 400 });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object);
        Assert.Equal(409, (await service.RetireTemplate(Company, "admin", "template", 2)).StatusCode);
    }

    [Fact]
    public async Task RetiredTemplateCannotBeUsedForNewAssignment()
    {
        var templates = Repository(new SalaryTemplate { Id = "template", CompanyId = Company, Name = "Engineer", EffectiveFrom = Start, IsRetired = true });
        var assignments = Repository<EmployeePayrollAssignment>();
        var service = new PayrollConfigurationService(Repository<PayrollPayGroup>().Object, assignments.Object, templates.Object, Repository<PayrollPolicyProfile>().Object);
        var result = await service.SaveAssignment(Company, "admin", new EmployeePayrollAssignmentDto { EmployeeId = "employee", SalaryTemplateId = "template", EffectiveFrom = Start });
        Assert.False(result.Success); Assert.Equal(404, result.StatusCode);
        assignments.Verify(repository => repository.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task TemplateFormulaValidationRunsOnServer()
    {
        var definition = Definition("travel", 1);
        definition.Code = "TRAVEL"; definition.CalculationType = "PERCENT_OF_COMPONENT"; definition.BaseComponentCode = "MISSING";
        var templates = Repository<SalaryTemplate>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object, Repository(definition).Object);
        var result = await service.SaveTemplate(Company, "admin", new SalaryTemplateDto { Name = "Invalid formula", EffectiveFrom = Start, BasicPay = 30000,
            Components = [new() { ComponentId = "travel", Code = "TRAVEL", Name = "Travel", Value = 10 }] });
        Assert.False(result.Success); Assert.Equal("PAYROLL_COMPONENT_BASE_MISSING", result.ErrorCode);
        templates.Verify(repository => repository.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    private static Mock<IMongoDbRepository<T>> Repository<T>(params T[] source)
    {
        var repository = new Mock<IMongoDbRepository<T>>();
        repository.Setup(value => value.GetAll(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<T, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<T>>(source.Where(predicate.Compile()).ToArray()));
        repository.Setup(value => value.FirstOrDefault(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<T, bool>> predicate, bool _, bool _) => Task.FromResult(source.FirstOrDefault(predicate.Compile())));
        repository.Setup(value => value.Exist(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<T, bool>> predicate, bool _) => Task.FromResult(source.Any(predicate.Compile())));
        repository.Setup(value => value.AddOne(It.IsAny<T>())).ReturnsAsync(new Result { Success = true });
        repository.Setup(value => value.Update(It.IsAny<FilterDefinition<T>>(), It.IsAny<T>())).ReturnsAsync(new Result { Success = true });
        return repository;
    }
}
