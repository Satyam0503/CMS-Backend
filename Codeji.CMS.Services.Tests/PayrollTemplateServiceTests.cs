using MongoDB.Driver;
using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollTemplateServiceTests
{
    [Fact]
    public async Task SavePolicy_RejectsUnsupportedDivisorBeforePersistence()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var templates = Repository<SalaryTemplate>();
        var groups = Repository<PayrollPayGroup>();
        var service = new PayrollTemplateService(policies.Object, templates.Object, groups.Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Name = "Invalid", DivisorPolicy = "UNKNOWN", EffectiveFrom = new DateTime(2026, 9, 1), IsActive = true
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        policies.Verify(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Theory]
    [InlineData(15, 30)]
    [InlineData(1, 15)]
    public async Task SavePolicy_RejectsPartialMonthEffectiveDates(int startDay, int endDay)
    {
        var policies = Repository<PayrollPolicyProfile>();
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "maker", new PayrollPolicyProfileDto
        {
            Name = "Monthly policy", EffectiveFrom = new DateTime(2026, 9, startDay),
            EffectiveTo = new DateTime(2026, 9, endDay), EpfMode = "EXEMPT", EsicMode = "EXEMPT", TdsMode = "MANUAL_ONLY"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("whole months", result.Message);
        policies.Verify(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_AllowsWholeMonthEffectiveDates()
    {
        var policies = Repository<PayrollPolicyProfile>();
        PayrollPolicyProfile? saved = null;
        policies.Setup(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()))
            .Callback<PayrollPolicyProfile>(value => saved = value)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "maker", new PayrollPolicyProfileDto
        {
            Name = "Monthly policy", EffectiveFrom = new DateTime(2026, 9, 1),
            EffectiveTo = new DateTime(2026, 9, 30), EpfMode = "EXEMPT", EsicMode = "EXEMPT", TdsMode = "MANUAL_ONLY"
        });

        Assert.True(result.Success);
        Assert.Equal(new DateTime(2026, 9, 30), saved?.EffectiveTo);
    }


    [Fact]
    public async Task SavePolicy_PersistsStatutoryModesAndCeilings()
    {
        var policies = Repository<PayrollPolicyProfile>();
        PayrollPolicyProfile? saved = null;
        policies.Setup(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()))
            .Callback<PayrollPolicyProfile>(value => saved = value)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Name = "Regional profile", EffectiveFrom = new DateTime(2026, 9, 1),
            EpfMode = "ACTUAL_BASIC_12", EpfWageCeiling = 22000m,
            StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09",
            EsicMode = "EXEMPT", EsicWageCeiling = 24000m, TdsMode = "MANUAL_ONLY"
        });

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("company-1", saved!.CompanyId);
        Assert.Equal("ACTUAL_BASIC_12", saved.EpfMode);
        Assert.Equal(22000m, saved.EpfWageCeiling);
        Assert.Equal("EXEMPT", saved.EsicMode);
        Assert.Equal(24000m, saved.EsicWageCeiling);
        Assert.Equal("MANUAL_ONLY", saved.TdsMode);
        Assert.Equal("IN-KA", saved.StatutoryJurisdiction);
        Assert.Equal("2026-27", saved.FinancialYear);
        Assert.Equal("reviewed-2026-09", saved.StatutoryRuleVersion);
    }

    [Fact]
    public async Task SavePolicy_RejectsActiveStatutoryPolicyWithoutReviewMetadata()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Name = "Missing review metadata", EffectiveFrom = new DateTime(2026, 9, 1),
            EpfMode = "CAPPED_CEILING", EsicMode = "STATUTORY", IsActive = true
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("jurisdiction", result.Message, StringComparison.OrdinalIgnoreCase);
        policies.Verify(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_RejectsAutomaticTdsWithoutReviewMetadataWhenOtherStatutoryModulesAreExempt()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Name = "TDS only", EffectiveFrom = new DateTime(2026, 9, 1), IsActive = true,
            EpfMode = "EXEMPT", EsicMode = "EXEMPT", TdsMode = "AUTO_NEW_REGIME"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("financial year", result.Message, StringComparison.OrdinalIgnoreCase);
        policies.Verify(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_KeepsAutomaticTdsOnlyProfileInDraftUntilApproval()
    {
        var policies = Repository<PayrollPolicyProfile>();
        PayrollPolicyProfile? saved = null;
        policies.Setup(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()))
            .Callback<PayrollPolicyProfile>(value => saved = value)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Name = "TDS only", EffectiveFrom = new DateTime(2026, 9, 1), IsActive = true,
            EpfMode = "EXEMPT", EsicMode = "EXEMPT", TdsMode = "AUTO_NEW_REGIME",
            StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09"
        });

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("DRAFT", saved!.ApprovalStatus);
        Assert.Equal("AUTO_NEW_REGIME", saved.TdsMode);
    }

    [Fact]
    public async Task SubmitPolicy_RejectsLegacyTdsOnlyDraftWithoutReviewMetadata()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var policy = new PayrollPolicyProfile
        {
            Id = "tds-draft", CompanyId = "company-1", Name = "Legacy TDS only", IsActive = true,
            ApprovalStatus = "DRAFT", Version = 1, EpfMode = "EXEMPT", EsicMode = "EXEMPT", TdsMode = "AUTO_NEW_REGIME"
        };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(policy);
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SubmitPolicy("company-1", "maker", policy.Id, 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DRAFT", policy.ApprovalStatus);
        policies.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task ApprovePolicy_RejectsLegacyTdsOnlySubmissionWithoutReviewMetadata()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var policy = new PayrollPolicyProfile
        {
            Id = "tds-submitted", CompanyId = "company-1", Name = "Legacy TDS only", IsActive = true,
            ApprovalStatus = "SUBMITTED", Version = 1, EpfMode = "EXEMPT", EsicMode = "EXEMPT", TdsMode = "AUTO_NEW_REGIME", SubmittedBy = "maker"
        };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(policy);
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.ApprovePolicy("company-1", "reviewer", policy.Id, 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("SUBMITTED", policy.ApprovalStatus);
        policies.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_RejectsInvalidStatutorySettings()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Name = "Invalid", EffectiveFrom = new DateTime(2026, 9, 1), EpfMode = "INVALID"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        policies.Verify(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_AllowsLaterCompanyRevisionWithSameNameAndAnOpenApprovedProfile()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var prior = new PayrollPolicyProfile { Id = "prior", CompanyId = "company-1", Name = "Company rules", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" };
        policies.Setup(x => x.Exist(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<PayrollPolicyProfile, bool>> predicate, bool _) => Task.FromResult(predicate.Compile()(prior)));
        PayrollPolicyProfile? saved = null;
        policies.Setup(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()))
            .Callback<PayrollPolicyProfile>(value => saved = value)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "maker", new PayrollPolicyProfileDto
        {
            Name = "Company rules", EffectiveFrom = new DateTime(2026, 9, 1),
            Scope = "COMPANY_DEFAULT", StatutoryJurisdiction = "IN-KA",
            FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09"
        });

        Assert.True(result.Success);
        Assert.Equal("DRAFT", saved?.ApprovalStatus);
        Assert.Equal("maker", saved?.CreatedBy);
        Assert.Equal("COMPANY_DEFAULT", saved?.Scope);
        Assert.Equal(new DateTime(2026, 9, 1), saved?.EffectiveFrom);
    }

    [Fact]
    public async Task SavePolicy_RejectsDuplicateNameAndStartDate()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var prior = new PayrollPolicyProfile { Id = "prior", CompanyId = "company-1", Name = "Company rules", EffectiveFrom = new DateTime(2026, 9, 1) };
        policies.Setup(x => x.Exist(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<PayrollPolicyProfile, bool>> predicate, bool _) => Task.FromResult(predicate.Compile()(prior)));
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "maker", new PayrollPolicyProfileDto
        {
            Name = "Company rules", EffectiveFrom = new DateTime(2026, 9, 1),
            StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09"
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        policies.Verify(x => x.AddOne(It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_RejectsMutationOfAnApprovedVersion()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var approved = new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Approved", ApprovalStatus = "APPROVED", Version = 3, EffectiveFrom = new DateTime(2026, 9, 1), EpfMode = "CAPPED_CEILING", EsicMode = "STATUTORY" };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(approved);
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Id = approved.Id, Version = approved.Version, Name = "Changed", EffectiveFrom = approved.EffectiveFrom,
            EpfMode = "ACTUAL_BASIC_12", EsicMode = "STATUTORY", StatutoryJurisdiction = "IN-KA",
            FinancialYear = "2026-27", StatutoryRuleVersion = "rules-2", IsActive = true
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Approved", approved.Name);
        policies.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }
    [Fact]
    public async Task SaveComponent_RejectsInvalidCodeBeforePersistence()
    {
        var components = Repository<PayrollSalaryComponentDefinition>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object, components.Object);

        var result = await service.SaveComponent("company-1", "actor", new PayrollSalaryComponentDto
        {
            Code = "not valid", Name = "Meal", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        components.Verify(x => x.AddOne(It.IsAny<PayrollSalaryComponentDefinition>()), Times.Never);
    }
    [Fact]
    public async Task ApprovePolicy_IsTenantScopedAndRequiresSubmittedState()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var policy = new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Statutory", ApprovalStatus = "SUBMITTED", Version = 2, EffectiveFrom = new DateTime(2026, 9, 1), StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09" };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(policy);
        policies.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.ApprovePolicy("company-1", "reviewer", "policy-1", 2);

        Assert.True(result.Success);
        Assert.Equal("APPROVED", policy.ApprovalStatus);
        Assert.Equal("reviewer", policy.ApprovedBy);
        Assert.NotNull(policy.ApprovedAtUtc);
    }

    [Fact]
    public async Task ApprovePolicy_RejectsSubmitterApprovingTheirOwnPolicy()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var policy = new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Statutory", ApprovalStatus = "SUBMITTED", SubmittedBy = "maker", Version = 2, EffectiveFrom = new DateTime(2026, 9, 1), StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09" };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(policy);
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.ApprovePolicy("company-1", "maker", "policy-1", 2);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("SUBMITTED", policy.ApprovalStatus);
        policies.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }

    [Fact]
    public async Task ApprovePolicy_RejectsCreatorEvenWhenAnotherUserSubmitted()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var policy = new PayrollPolicyProfile
        {
            Id = "policy-1", CompanyId = "company-1", Name = "Statutory", ApprovalStatus = "SUBMITTED",
            CreatedBy = "maker", SubmittedBy = "reviewer", Version = 2, EffectiveFrom = new DateTime(2026, 9, 1),
            StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09"
        };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(policy);
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.ApprovePolicy("company-1", "maker", policy.Id, policy.Version);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        policies.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()), Times.Never);
    }
    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>()
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        repository.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}
