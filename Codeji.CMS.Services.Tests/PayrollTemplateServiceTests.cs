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
    public async Task DeletePolicyDraft_DeletesOnlyMatchingCompanyDraftVersion()
    {
        var policies = Repository<PayrollPolicyProfile>();
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Draft", ApprovalStatus = "DRAFT", Version = 3 });
        policies.Setup(x => x.Delete(It.IsAny<FilterDefinition<PayrollPolicyProfile>>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.DeletePolicyDraft("company-1", "policy-1", 3);

        Assert.True(result.Success);
        policies.Verify(x => x.Delete(It.IsAny<FilterDefinition<PayrollPolicyProfile>>()), Times.Once);
    }

    [Theory]
    [InlineData("SUBMITTED")]
    [InlineData("APPROVED")]
    public async Task DeletePolicyDraft_PreservesSubmittedAndApprovedHistory(string status)
    {
        var policies = Repository<PayrollPolicyProfile>();
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Policy", ApprovalStatus = status, Version = 3 });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.DeletePolicyDraft("company-1", "policy-1", 3);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        policies.Verify(x => x.Delete(It.IsAny<FilterDefinition<PayrollPolicyProfile>>()), Times.Never);
    }

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
    public async Task SavePolicy_RejectsEditingApprovedVersionAndRequiresNewDatedVersion()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var approved = new PayrollPolicyProfile
        {
            Id = "approved-policy", CompanyId = "company-1", Name = "Company rules",
            Version = 4, ApprovalStatus = "APPROVED", EffectiveFrom = new DateTime(2026, 9, 1),
            EpfMode = "EXEMPT", EsicMode = "EXEMPT", TdsMode = "MANUAL_ONLY"
        };
        policies.Setup(x => x.FirstOrDefault(
                It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), false, true))
            .ReturnsAsync(approved);
        var service = new PayrollTemplateService(
            policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "actor", new PayrollPolicyProfileDto
        {
            Id = approved.Id, Version = approved.Version, Name = approved.Name,
            EffectiveFrom = approved.EffectiveFrom, EpfMode = "EXEMPT", EsicMode = "EXEMPT",
            TdsMode = "MANUAL_ONLY", EpfEmployeeRatePercent = 10m
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("immutable", result.Message, StringComparison.OrdinalIgnoreCase);
        policies.Verify(x => x.Update(
            It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()), Times.Never);
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
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), false, true)).ReturnsAsync(approved);
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
    public async Task SaveComponent_AppendsNewEffectiveVersionWithoutRewritingHistory()
    {
        var prior = new PayrollSalaryComponentDefinition
        {
            Id = "bonus-v1", CompanyId = "company-1", Code = "BONUS", Name = "Bonus",
            Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true,
            EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1
        };
        var components = Repository<PayrollSalaryComponentDefinition>();
        PayrollSalaryComponentDefinition? closedPrior = null;
        components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<PayrollSalaryComponentDefinition, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<PayrollSalaryComponentDefinition>>(new[] { prior }.Where(predicate.Compile()).ToArray()));
        components.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollSalaryComponentDefinition>>(), It.IsAny<PayrollSalaryComponentDefinition>()))
            .Callback<FilterDefinition<PayrollSalaryComponentDefinition>, PayrollSalaryComponentDefinition>((_, value) => closedPrior = value)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object, components.Object);

        var result = await service.SaveComponent("company-1", "actor", new PayrollSalaryComponentDto
        {
            Id = prior.Id, Version = prior.Version, Code = "BONUS", Name = "Bonus",
            Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true,
            EffectiveFrom = new DateTime(2026, 10, 5), IsActive = false
        });

        Assert.True(result.Success);
        Assert.True(prior.IsActive);
        Assert.Equal(new DateTime(2026, 1, 1), prior.EffectiveFrom);
        Assert.Equal(new DateTime(2026, 10, 4), closedPrior?.EffectiveTo);
        components.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollSalaryComponentDefinition>>(), It.IsAny<PayrollSalaryComponentDefinition>()), Times.Once);
        components.Verify(x => x.AddOne(It.Is<PayrollSalaryComponentDefinition>(revision =>
            revision.Id == string.Empty && revision.CompanyId == "company-1" && revision.Code == "BONUS" &&
            revision.EffectiveFrom == new DateTime(2026, 10, 5) && !revision.IsActive && revision.Version == 2)), Times.Once);
    }

    [Fact]
    public async Task SaveComponent_RejectsRevisionAtOrBeforeLatestStart()
    {
        var prior = new PayrollSalaryComponentDefinition
        {
            Id = "meal-v1", CompanyId = "company-1", Code = "MEAL", Name = "Meal",
            Kind = "EARNING", CalculationType = "FIXED", EffectiveFrom = new DateTime(2026, 10, 8),
            IsActive = true, Version = 1
        };
        var components = Repository<PayrollSalaryComponentDefinition>();
        components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([prior]);
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object, components.Object);

        var result = await service.SaveComponent("company-1", "actor", new PayrollSalaryComponentDto
        {
            Id = prior.Id, Version = prior.Version, Code = "MEAL", Name = "Meal",
            Kind = "EARNING", CalculationType = "FIXED", EffectiveFrom = prior.EffectiveFrom
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        components.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollSalaryComponentDefinition>>(), It.IsAny<PayrollSalaryComponentDefinition>()), Times.Never);
    }

    [Fact]
    public async Task SaveTemplate_RejectsComponentDisabledAtTemplateStart()
    {
        var definitions = new[]
        {
            new PayrollSalaryComponentDefinition { Id = "bonus-v1", CompanyId = "company-1", Code = "BONUS", Name = "Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1 },
            new PayrollSalaryComponentDefinition { Id = "bonus-v2", CompanyId = "company-1", Code = "BONUS", Name = "Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = new DateTime(2026, 10, 1), IsActive = false, Version = 2 }
        };
        var components = Repository<PayrollSalaryComponentDefinition>();
        components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<PayrollSalaryComponentDefinition, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<PayrollSalaryComponentDefinition>>(definitions.Where(predicate.Compile()).ToArray()));
        var templates = Repository<SalaryTemplate>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object, components.Object);

        var result = await service.SaveTemplate("company-1", "actor", new SalaryTemplateDto
        {
            Name = "Monthly salary", EffectiveFrom = new DateTime(2026, 10, 1),
            Components = [new PayrollSalaryTemplateComponentDto { ComponentId = "bonus-v1", Code = "BONUS", Name = "Bonus", Value = 1000m }]
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        templates.Verify(x => x.AddOne(It.IsAny<SalaryTemplate>()), Times.Never);
    }

    [Fact]
    public async Task SaveTemplate_MapsTheEligibleHistoricalComponentVersionWhenTemplateEndsBeforeChange()
    {
        var definitions = new[]
        {
            new PayrollSalaryComponentDefinition { Id = "bonus-v1", CompanyId = "company-1", Code = "BONUS", Name = "Bonus v1", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1 },
            new PayrollSalaryComponentDefinition { Id = "bonus-v2", CompanyId = "company-1", Code = "BONUS", Name = "Bonus v2", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = false, EffectiveFrom = new DateTime(2026, 10, 1), IsActive = true, Version = 2 }
        };
        var components = Repository<PayrollSalaryComponentDefinition>();
        components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<PayrollSalaryComponentDefinition, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<PayrollSalaryComponentDefinition>>(definitions.Where(predicate.Compile()).ToArray()));
        var templates = Repository<SalaryTemplate>();
        SalaryTemplate? saved = null;
        templates.Setup(x => x.AddOne(It.IsAny<SalaryTemplate>())).Callback<SalaryTemplate>(value => saved = value).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object, components.Object);

        var result = await service.SaveTemplate("company-1", "actor", new SalaryTemplateDto
        {
            Name = "Q3 salary", EffectiveFrom = new DateTime(2026, 7, 1), EffectiveTo = new DateTime(2026, 9, 30),
            Components = [new PayrollSalaryTemplateComponentDto { ComponentId = "bonus-v1", Code = "BONUS", Name = "Bonus", Value = 1000m }]
        });

        Assert.True(result.Success);
        Assert.Equal("bonus-v1", saved?.Components.Single().ComponentId);
        Assert.Equal("Bonus v1", saved?.Components.Single().Name);
        Assert.True(saved?.Components.Single().IsTaxable);
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

    [Fact]
    public async Task ApprovePolicy_AllowsCompanyAdministratorToApproveTheirOwnSubmission()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var policy = new PayrollPolicyProfile
        {
            Id = "policy-1", CompanyId = "company-1", Name = "Statutory", ApprovalStatus = "SUBMITTED",
            CreatedBy = "admin", SubmittedBy = "admin", Version = 2, EffectiveFrom = new DateTime(2026, 9, 1),
            StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "reviewed-2026-09"
        };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(policy);
        policies.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.ApprovePolicy("company-1", "admin", "policy-1", 2, isCompanyAdministrator: true);

        Assert.True(result.Success);
        Assert.Equal("APPROVED", policy.ApprovalStatus);
        Assert.Equal("admin", policy.ApprovedBy);
        Assert.NotNull(policy.ApprovedAtUtc);
    }
    [Fact]
    public async Task SavePolicy_AllowsEditingSubmittedPolicyInPlaceAndResetsToDraft()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var submitted = new PayrollPolicyProfile
        {
            Id = "sub-1", CompanyId = "company-1", Name = "Submitted Policy", ApprovalStatus = "SUBMITTED",
            Version = 2, EffectiveFrom = new DateTime(2026, 9, 1), EpfMode = "CAPPED_CEILING", EsicMode = "STATUTORY",
            StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "rules-1"
        };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(submitted);
        PayrollPolicyProfile? updated = null;
        policies.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()))
            .Callback<FilterDefinition<PayrollPolicyProfile>, PayrollPolicyProfile>((_, val) => updated = val)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.SavePolicy("company-1", "maker", new PayrollPolicyProfileDto
        {
            Id = submitted.Id, Version = submitted.Version, Name = "Corrected Submitted Policy",
            EffectiveFrom = submitted.EffectiveFrom, EpfMode = "CAPPED_CEILING", EsicMode = "STATUTORY",
            StatutoryJurisdiction = "IN-KA", FinancialYear = "2026-27", StatutoryRuleVersion = "rules-1"
        });

        Assert.True(result.Success);
        Assert.NotNull(updated);
        Assert.Equal("DRAFT", updated!.ApprovalStatus);
        Assert.Equal("Corrected Submitted Policy", updated.Name);
        Assert.Equal(3, updated.Version);
    }

    [Fact]
    public async Task CancelPolicySubmission_WithdrawsSubmittedPolicyToDraft()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var submitted = new PayrollPolicyProfile
        {
            Id = "sub-1", CompanyId = "company-1", Name = "Submitted Policy", ApprovalStatus = "SUBMITTED",
            SubmittedBy = "maker", Version = 2, EffectiveFrom = new DateTime(2026, 9, 1)
        };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(submitted);
        policies.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.CancelPolicySubmission("company-1", "maker", "sub-1", 2);

        Assert.True(result.Success);
        Assert.Equal("DRAFT", submitted.ApprovalStatus);
        Assert.Null(submitted.SubmittedBy);
        Assert.Equal(3, submitted.Version);
    }

    [Fact]
    public async Task DeleteTemplate_RejectsWhenAssignedToEmployees()
    {
        var template = new SalaryTemplate { Id = "tpl-1", CompanyId = "company-1", Name = "Standard", Version = 1 };
        var templates = Repository<SalaryTemplate>();
        templates.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<SalaryTemplate, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(template);
        var assignments = Repository<EmployeePayrollAssignment>();
        assignments.Setup(x => x.Exist(It.IsAny<Expression<Func<EmployeePayrollAssignment, bool>>>(), It.IsAny<bool>())).ReturnsAsync(true);
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object, null, null, assignments.Object);

        var result = await service.DeleteTemplate("company-1", "admin", "tpl-1", 1);
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("employees are assigned to it", result.Message);
    }

    [Fact]
    public async Task DeleteTemplate_DeletesWhenUnused()
    {
        var template = new SalaryTemplate { Id = "tpl-1", CompanyId = "company-1", Name = "Standard", Version = 1 };
        var templates = Repository<SalaryTemplate>();
        templates.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<SalaryTemplate, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(template);
        templates.Setup(x => x.Delete(It.IsAny<FilterDefinition<SalaryTemplate>>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var assignments = Repository<EmployeePayrollAssignment>();
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, templates.Object, Repository<PayrollPayGroup>().Object, null, null, assignments.Object);

        var result = await service.DeleteTemplate("company-1", "admin", "tpl-1", 1);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task RetirePolicy_DeactivatesActivePolicyAndSetsEffectiveTo()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var approved = new PayrollPolicyProfile
        {
            Id = "app-1", CompanyId = "company-1", Name = "Approved Active", ApprovalStatus = "APPROVED",
            IsActive = true, Version = 2, EffectiveFrom = new DateTime(2026, 1, 1)
        };
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>())).ReturnsAsync(approved);
        policies.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollPolicyProfile>>(), It.IsAny<PayrollPolicyProfile>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollTemplateService(policies.Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);

        var result = await service.RetirePolicy("company-1", "admin", "app-1", 2);

        Assert.True(result.Success);
        Assert.False(approved.IsActive);
        Assert.NotNull(approved.EffectiveTo);
        Assert.Equal(3, approved.Version);
    }


    [Fact]
    public async Task PreviewTemplate_CalculatesCtcGrossDeductionsAndNet_WithExplanationNote()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var templates = Repository<SalaryTemplate>();
        var groups = Repository<PayrollPayGroup>();
        var components = Repository<PayrollSalaryComponentDefinition>();

        components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>
            {
                new() { Id = "c-pf", CompanyId = "company-1", Code = "ER_PF", Name = "Employer PF", Kind = "EARNING", CalculationType = "FIXED", IsEmployerContribution = true, IsRecurring = true, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1 }
            });

        var service = new PayrollTemplateService(policies.Object, templates.Object, groups.Object, components.Object);

        var preview = await service.PreviewTemplate("company-1", new SalaryTemplateDto
        {
            Name = "Software Engineer",
            EffectiveFrom = new DateTime(2026, 4, 1),
            BasicPay = 50000m,
            Hra = 20000m,
            Components =
            [
                new PayrollSalaryTemplateComponentDto
                {
                    ComponentId = "c-pf",
                    Code = "ER_PF",
                    Name = "Employer PF",
                    Kind = "EARNING",
                    CalculationType = "FIXED",
                    Value = 6000m,
                    IsEmployerContribution = true,
                    IsRecurring = true
                }
            ]
        });

        Assert.True(preview.Success);
        Assert.NotNull(preview.Data);
        Assert.Equal(70000m, preview.Data.MonthlyGross);
        Assert.Equal(840000m, preview.Data.AnnualGross);
        Assert.Equal(6000m, preview.Data.MonthlyEmployerContributions);
        Assert.Equal(72000m, preview.Data.AnnualEmployerContributions);
        Assert.Equal(76000m, preview.Data.MonthlyTotalCtc);
        Assert.Equal(912000m, preview.Data.AnnualTotalCtc);
        Assert.Equal(70000m, preview.Data.IndicativeMonthlyTakeHome);
        Assert.Contains("Indicative take-home", preview.Data.ExplanatoryNote);
    }

    [Fact]
    public async Task SaveTemplate_RejectsConflictingLegacyAndComponentValues_With409Conflict()
    {
        var policies = Repository<PayrollPolicyProfile>();
        var templates = Repository<SalaryTemplate>();
        var groups = Repository<PayrollPayGroup>();
        var components = Repository<PayrollSalaryComponentDefinition>();

        components.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>
            {
                new() { Id = "c-basic", CompanyId = "company-1", Code = "BASIC", Name = "Basic Pay", Kind = "EARNING", CalculationType = "FIXED", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1 }
            });

        var service = new PayrollTemplateService(policies.Object, templates.Object, groups.Object, components.Object);

        var result = await service.SaveTemplate("company-1", "admin", new SalaryTemplateDto
        {
            Name = "Conflicting Template",
            EffectiveFrom = new DateTime(2026, 4, 1),
            BasicPay = 50000m,
            Components =
            [
                new PayrollSalaryTemplateComponentDto
                {
                    ComponentId = "c-basic",
                    Code = "BASIC",
                    Name = "Basic Pay",
                    Kind = "EARNING",
                    CalculationType = "FIXED",
                    Value = 60000m // Conflicting with BasicPay = 50000m
                }
            ]
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("conflicting amounts", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewTemplate_RejectsNegativeAmounts_With400BadRequest()
    {
        var service = new PayrollTemplateService(Repository<PayrollPolicyProfile>().Object, Repository<SalaryTemplate>().Object, Repository<PayrollPayGroup>().Object);
        var result = await service.PreviewTemplate("company-1", new SalaryTemplateDto
        {
            Name = "Negative Template",
            BasicPay = -5000m,
            EffectiveFrom = new DateTime(2026, 4, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task SeedStandardComponents_PopulatesStatutoryTaxAndProrationClassifications()
    {
        var componentsRepo = Repository<PayrollSalaryComponentDefinition>();
        componentsRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollSalaryComponentDefinition, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollSalaryComponentDefinition>());

        var seededList = new List<PayrollSalaryComponentDefinition>();
        componentsRepo.Setup(x => x.AddOne(It.IsAny<PayrollSalaryComponentDefinition>()))
            .Callback<PayrollSalaryComponentDefinition>(seededList.Add)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var service = new PayrollTemplateService(
            Repository<PayrollPolicyProfile>().Object,
            Repository<SalaryTemplate>().Object,
            Repository<PayrollPayGroup>().Object,
            componentsRepo.Object);

        var result = await service.SeedStandardComponents("company-1", "admin");
        Assert.True(result.Success);
        Assert.Equal(5, seededList.Count);

        var hra = seededList.First(x => x.Code == "HRA");
        Assert.Equal("10(13A)", hra.TaxExemptionSection);
        Assert.Equal("ATTENDANCE", hra.ProrationBasis);
        Assert.True(hra.IncludeInPtWage);
        Assert.False(hra.IncludeInPfWage);

        var bonus = seededList.First(x => x.Code == "BONUS");
        Assert.Equal("NOT_PRORATED", bonus.ProrationBasis);
        Assert.True(bonus.IsRecurring);
        Assert.True(bonus.IncludeInEsiWage);

        var health = seededList.First(x => x.Code == "HEALTH_INSURANCE");
        Assert.Equal("80D", health.TaxExemptionSection);
        Assert.Equal("DEDUCTION", health.Kind);
    }
    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>()
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        repository.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}
