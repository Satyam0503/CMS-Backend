using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class TaxDeclarationServiceTests
{
    private const string TenantId = "tenant-1";
    private const string EmpId = "EMP001";
    private const string FY = "2025-26";
    private const string Actor = "user-1";

    [Fact]
    public async Task SubmitDeclaration_CreatesNewDeclaration_WithVersion1()
    {
        var repo = Repo<EmployeeTaxDeclaration>();
        var service = new TaxDeclarationService(repo.Object);

        var request = new SubmitTaxDeclarationRequestDto
        {
            FinancialYear = FY,
            Regime = "OLD_REGIME",
            PreviousEmployerGrossSalary = 200000m,
            PreviousEmployerTds = 15000m,
            DeclaredSection80C = 180000m,
            DeclaredSection80D = 30000m,
            DeclaredSection24HomeLoanInterest = 250000m
        };

        var result = await service.SubmitDeclaration(TenantId, EmpId, request, Actor);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(1, result.MethodResult.Version);
        Assert.Equal("SUBMITTED", result.MethodResult.Status);
        Assert.Equal("OLD_REGIME", result.MethodResult.Regime);
        Assert.Equal(200000m, result.MethodResult.PreviousEmployerGrossSalary);
        Assert.Equal(180000m, result.MethodResult.DeclaredSection80C);
        Assert.Single(result.MethodResult.AuditHistory);
    }

    [Fact]
    public async Task SubmitDeclaration_WhenConcurrentVersionInsertConflicts_Returns409()
    {
        var repo = Repo<EmployeeTaxDeclaration>();
        repo.Setup(x => x.AddOne(It.IsAny<EmployeeTaxDeclaration>()))
            .ReturnsAsync(new Result { Success = false, StatusCode = 409 });
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.SubmitDeclaration(TenantId, EmpId,
            new SubmitTaxDeclarationRequestDto { FinancialYear = FY }, Actor);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("version already exists", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitDeclaration_UsesVersionAndStatusCompareAndSwapForRejectedRevision()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
            Status = "REJECTED", Version = 4
        };
        var repo = Repo(existing);
        MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>? attemptedFilter = null;
        repo.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>>(), It.IsAny<EmployeeTaxDeclaration>()))
            .Callback<MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>, EmployeeTaxDeclaration>((filter, _) => attemptedFilter = filter)
            .ReturnsAsync(new Result { Success = false, StatusCode = 400 });
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.SubmitDeclaration(TenantId, EmpId,
            new SubmitTaxDeclarationRequestDto { FinancialYear = FY }, Actor);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        var filter = Render(attemptedFilter!);
        Assert.Contains("Version", filter.ToJson(), StringComparison.Ordinal);
        Assert.Contains("Status", filter.ToJson(), StringComparison.Ordinal);
        Assert.Contains("REJECTED", filter.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReviewDeclaration_WhenCompareAndSwapLosesRace_Returns409()
    {
        var submitted = new EmployeeTaxDeclaration
        {
            Id = "decl-1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
            Status = "SUBMITTED", Version = 2, SubmittedBy = "employee-1"
        };
        var repo = Repo(submitted);
        MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>? attemptedFilter = null;
        repo.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>>(), It.IsAny<EmployeeTaxDeclaration>()))
            .Callback<MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>, EmployeeTaxDeclaration>((filter, _) => attemptedFilter = filter)
            .ReturnsAsync(new Result { Success = false, StatusCode = 400 });
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.ReviewDeclaration(TenantId, "decl-1",
            new ReviewTaxDeclarationRequestDto { Decision = "APPROVE" }, "reviewer-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        var filter = Render(attemptedFilter!);
        Assert.Contains("Version", filter.ToJson(), StringComparison.Ordinal);
        Assert.Contains("SUBMITTED", filter.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitDeclaration_UpdatesExistingDeclaration_IncrementsVersion()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1",
            CompanyId = TenantId,
            EmployeeId = EmpId,
            FinancialYear = FY,
            Regime = "NEW_115BAC",
            Status = "REJECTED",
            Version = 1
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var request = new SubmitTaxDeclarationRequestDto
        {
            FinancialYear = FY,
            Regime = "OLD_REGIME",
            DeclaredSection80C = 150000m
        };

        var result = await service.SubmitDeclaration(TenantId, EmpId, request, Actor);

        Assert.True(result.Success);
        Assert.Equal(2, result.MethodResult!.Version);
        Assert.Equal("OLD_REGIME", result.MethodResult.Regime);
        Assert.Equal(150000m, result.MethodResult.DeclaredSection80C);
    }

    [Fact]
    public async Task SubmitDeclaration_RevisionClearsPreviousReviewAndEvidenceVerification()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
            Status = "REJECTED", Version = 1, ApprovedSection80C = 10000m,
            ReviewedBy = "reviewer", RejectionReason = "Missing document",
            ProofAttachments = [new TaxProofAttachment { Id = "old", IsVerified = true }]
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.SubmitDeclaration(TenantId, EmpId,
            new SubmitTaxDeclarationRequestDto { FinancialYear = FY }, Actor);

        Assert.True(result.Success);
        Assert.Equal(0m, result.MethodResult!.ApprovedSection80C);
        Assert.Null(result.MethodResult.ReviewedBy);
        Assert.Null(result.MethodResult.RejectionReason);
        Assert.Empty(result.MethodResult.ProofAttachments);
    }

    [Fact]
    public async Task SubmitDeclaration_RejectsInvalidRegime()
    {
        var repo = Repo<EmployeeTaxDeclaration>();
        var service = new TaxDeclarationService(repo.Object);

        var request = new SubmitTaxDeclarationRequestDto
        {
            FinancialYear = FY,
            Regime = "INVALID_REGIME"
        };

        var result = await service.SubmitDeclaration(TenantId, EmpId, request, Actor);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task SubmitDeclaration_RejectsNegativeAmountsInsteadOfSilentlyClamping()
    {
        var repo = Repo<EmployeeTaxDeclaration>();
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.SubmitDeclaration(TenantId, EmpId, new SubmitTaxDeclarationRequestDto
        {
            FinancialYear = FY,
            DeclaredSection80C = -1m
        }, Actor);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        repo.Verify(x => x.AddOne(It.IsAny<EmployeeTaxDeclaration>()), Times.Never);
    }

    [Fact]
    public async Task SubmitDeclaration_RejectsInvalidFinancialYearRange()
    {
        var repo = Repo<EmployeeTaxDeclaration>();
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.SubmitDeclaration(TenantId, EmpId,
            new SubmitTaxDeclarationRequestDto { FinancialYear = "2025-29" }, Actor);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Theory]
    [InlineData("SUBMITTED")]
    public async Task SubmitDeclaration_RequiresReviewBeforeRevision(string status)
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY, Status = status
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.SubmitDeclaration(TenantId, EmpId,
            new SubmitTaxDeclarationRequestDto { FinancialYear = FY }, Actor);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        repo.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>>(), It.IsAny<EmployeeTaxDeclaration>()), Times.Never);
    }

    [Fact]
    public async Task SubmitDeclaration_AfterApprovalCreatesLinkedVersionAndPreservesPayrollApproval()
    {
        var approved = new EmployeeTaxDeclaration
        {
            Id = "approved-1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
            Status = "APPROVED", Version = 1, ApprovedSection80C = 50000m
        };
        var repo = Repo(approved);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.SubmitDeclaration(TenantId, EmpId,
            new SubmitTaxDeclarationRequestDto { FinancialYear = FY, DeclaredSection80C = 70000m }, Actor);

        Assert.True(result.Success);
        Assert.Equal(2, result.MethodResult!.Version);
        Assert.Equal("approved-1", result.MethodResult.PreviousVersionId);
        Assert.Equal("SUBMITTED", result.MethodResult.Status);
        Assert.Equal("APPROVED", approved.Status);
        Assert.Equal(50000m, approved.ApprovedSection80C);
        repo.Verify(x => x.AddOne(It.Is<EmployeeTaxDeclaration>(d => d.Version == 2 && d.PreviousVersionId == "approved-1")), Times.Once);
        repo.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>>(), It.IsAny<EmployeeTaxDeclaration>()), Times.Never);
    }

    [Fact]
    public async Task ReviewDeclaration_RejectsWithoutReason_Returns400()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1",
            CompanyId = TenantId,
            EmployeeId = EmpId,
            FinancialYear = FY,
            Status = "SUBMITTED"
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.ReviewDeclaration(TenantId, "decl-1", new ReviewTaxDeclarationRequestDto
        {
            Decision = "REJECT",
            RejectionReason = ""
        }, Actor);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ReviewDeclaration_ApprovesOldRegime_EnforcesStatutoryCaps()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1",
            CompanyId = TenantId,
            EmployeeId = EmpId,
            FinancialYear = FY,
            Regime = "OLD_REGIME",
            Status = "SUBMITTED",
            DeclaredSection80C = 250000m, // Excess of 1.5L
            DeclaredSection80Ccd1B = 80000m, // Excess of 50k
            DeclaredSection80D = 150000m, // Excess of 100k
            DeclaredSection24HomeLoanInterest = 350000m // Excess of 2L
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.ReviewDeclaration(TenantId, "decl-1", new ReviewTaxDeclarationRequestDto
        {
            Decision = "APPROVE",
            ReviewRemarks = "Verified receipts"
        }, Actor);

        Assert.True(result.Success);
        Assert.Equal("APPROVED", result.MethodResult!.Status);
        // Statutory caps enforced:
        Assert.Equal(150000m, result.MethodResult.ApprovedSection80C);
        Assert.Equal(50000m, result.MethodResult.ApprovedSection80Ccd1B);
        Assert.Equal(100000m, result.MethodResult.ApprovedSection80D);
        Assert.Equal(200000m, result.MethodResult.ApprovedSection24HomeLoanInterest);
    }

    [Fact]
    public async Task ReviewDeclaration_CannotApproveMoreThanTheEmployeeDeclared()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
            Regime = "OLD_REGIME", Status = "SUBMITTED", SubmittedBy = "employee-1",
            DeclaredSection80C = 10000m
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.ReviewDeclaration(TenantId, "decl-1", new ReviewTaxDeclarationRequestDto
        {
            Decision = "APPROVE", ApprovedSection80C = 50000m
        }, Actor);

        Assert.True(result.Success);
        Assert.Equal(10000m, result.MethodResult!.ApprovedSection80C);
    }

    [Fact]
    public async Task ReviewDeclaration_SubmitterCannotApproveOwnDeclaration()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
            Status = "SUBMITTED", SubmittedBy = Actor
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.ReviewDeclaration(TenantId, "decl-1",
            new ReviewTaxDeclarationRequestDto { Decision = "APPROVE" }, Actor);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        repo.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmployeeTaxDeclaration>>(), It.IsAny<EmployeeTaxDeclaration>()), Times.Never);
    }

    [Fact]
    public async Task ReviewDeclaration_ApprovesNewRegime_SetsChapterViaDeductionsToZero()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1",
            CompanyId = TenantId,
            EmployeeId = EmpId,
            FinancialYear = FY,
            Regime = "NEW_115BAC",
            Status = "SUBMITTED",
            DeclaredSection80C = 150000m,
            DeclaredSection80D = 25000m
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.ReviewDeclaration(TenantId, "decl-1", new ReviewTaxDeclarationRequestDto
        {
            Decision = "APPROVE"
        }, Actor);

        Assert.True(result.Success);
        Assert.Equal("APPROVED", result.MethodResult!.Status);
        // Chapter VI-A deductions are 0 in New Regime (Section 115BAC)
        Assert.Equal(0m, result.MethodResult.ApprovedSection80C);
        Assert.Equal(0m, result.MethodResult.ApprovedSection80D);
        Assert.Equal(0m, result.MethodResult.ApprovedSection24HomeLoanInterest);
    }

    [Fact]
    public async Task ReviewDeclaration_VerifiesSpecifiedAttachments()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1",
            CompanyId = TenantId,
            EmployeeId = EmpId,
            FinancialYear = FY,
            Regime = "OLD_REGIME",
            Status = "SUBMITTED",
            ProofAttachments =
            [
                new TaxProofAttachment { Id = "att-1", Section = "80C", DocumentName = "lic.pdf", Amount = 50000m, IsVerified = false },
                new TaxProofAttachment { Id = "att-2", Section = "80D", DocumentName = "health.pdf", Amount = 20000m, IsVerified = false }
            ]
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.ReviewDeclaration(TenantId, "decl-1", new ReviewTaxDeclarationRequestDto
        {
            Decision = "APPROVE",
            VerifiedAttachmentIds = ["att-1"]
        }, Actor);

        Assert.True(result.Success);
        var att1 = result.MethodResult!.ProofAttachments.First(a => a.Id == "att-1");
        var att2 = result.MethodResult.ProofAttachments.First(a => a.Id == "att-2");
        Assert.True(att1.IsVerified);
        Assert.False(att2.IsVerified);
    }

    [Fact]
    public async Task CalculateTaxProjection_CalculatesAccurateOldRegimeTax()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1",
            CompanyId = TenantId,
            EmployeeId = EmpId,
            FinancialYear = FY,
            Regime = "OLD_REGIME",
            Status = "APPROVED",
            ApprovedSection80C = 150000m,
            ApprovedSection80D = 25000m
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        // Annual gross 12L:
        // Total Deductions = 50k (std) + 150k (80C) + 25k (80D) = 225k
        // Taxable Income = 12L - 2.25L = 9.75L
        // Tax in Old Regime on 9.75L:
        // 0 to 2.5L: Nil
        // 2.5L to 5L: 12,500 (5%)
        // 5L to 9.75L (4.75L): 95,000 (20%)
        // Total tax before cess = 107,500
        // 4% cess = 4,300
        // Total Annual Tax = 111,800
        var result = await service.CalculateTaxProjection(TenantId, EmpId, FY, 1200000m);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(1200000m, result.MethodResult.GrossSalaryFromThisEmployer);
        Assert.Equal(225000m, result.MethodResult.TotalDeductions);
        Assert.Equal(975000m, result.MethodResult.NetTaxableIncome);
        Assert.Equal(111800m, result.MethodResult.TotalAnnualTax);
        Assert.InRange(result.MethodResult.RemainingMonths, 1, 12);
        Assert.Equal(PayrollCalculationRules.RoundMoney(111800m / result.MethodResult.RemainingMonths), result.MethodResult.MonthlyTds);
    }

    [Fact]
    public async Task CalculateTaxProjection_UsesLastApprovedVersionWhileNewRevisionIsPending()
    {
        var declarations = new[]
        {
            new EmployeeTaxDeclaration
            {
                Id = "approved-v1", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
                Regime = "OLD_REGIME", Status = "APPROVED", Version = 1,
                ApprovedSection80C = 50000m
            },
            new EmployeeTaxDeclaration
            {
                Id = "pending-v2", CompanyId = TenantId, EmployeeId = EmpId, FinancialYear = FY,
                Regime = "NEW_115BAC", Status = "SUBMITTED", Version = 2,
                PreviousVersionId = "approved-v1", DeclaredSection80C = 150000m
            }
        };
        var repo = Repo(declarations);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.CalculateTaxProjection(TenantId, EmpId, FY, 1200000m);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("OLD_REGIME", result.MethodResult.Regime);
        Assert.Equal(50000m, result.MethodResult.Section80C);
        Assert.Equal(100000m, result.MethodResult.TotalDeductions); // standard deduction + approved 80C
    }

    [Fact]
    public async Task CalculateTaxProjection_CalculatesAccurateNewRegimeTaxWithRebate()
    {
        var existing = new EmployeeTaxDeclaration
        {
            Id = "decl-1",
            CompanyId = TenantId,
            EmployeeId = EmpId,
            FinancialYear = FY,
            Regime = "NEW_115BAC",
            Status = "APPROVED"
        };
        var repo = Repo(existing);
        var service = new TaxDeclarationService(repo.Object);

        // Gross 12L under New Regime:
        // Taxable = 12L - 75k (std deduction) = 11.25L
        // <= 12L rebate threshold: Full rebate u/s 87A! Total Tax = 0!
        var result = await service.CalculateTaxProjection(TenantId, EmpId, FY, 1200000m);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(75000m, result.MethodResult.StandardDeduction);
        Assert.Equal(1125000m, result.MethodResult.NetTaxableIncome);
        Assert.Equal(0m, result.MethodResult.TotalAnnualTax);
        Assert.Equal(0m, result.MethodResult.MonthlyTds);
    }

    [Fact]
    public async Task CalculateTaxProjection_RefusesUncataloguedTaxYear()
    {
        var repo = Repo<EmployeeTaxDeclaration>();
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.CalculateTaxProjection(TenantId, EmpId, "2027-28", 1200000m);

        Assert.False(result.Success);
        Assert.Equal(422, result.StatusCode);
        Assert.Contains("rules for 2027-28 are not available", result.Message, StringComparison.OrdinalIgnoreCase);
        repo.Verify(x => x.GetAll(It.IsAny<Expression<Func<EmployeeTaxDeclaration, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GetDeclaration_ReturnsNotFound_WhenNoRecord()
    {
        var repo = Repo<EmployeeTaxDeclaration>();
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.GetDeclaration(TenantId, "EMP_UNKNOWN", FY);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetCompanyDeclarations_FiltersByCompanyAndStatus()
    {
        var declarations = new[]
        {
            new EmployeeTaxDeclaration { Id = "d1", CompanyId = TenantId, EmployeeId = "E1", FinancialYear = FY, Status = "APPROVED" },
            new EmployeeTaxDeclaration { Id = "d2", CompanyId = TenantId, EmployeeId = "E2", FinancialYear = FY, Status = "SUBMITTED" },
            new EmployeeTaxDeclaration { Id = "d3", CompanyId = "other-tenant", EmployeeId = "E3", FinancialYear = FY, Status = "APPROVED" }
        };
        var repo = Repo(declarations);
        var service = new TaxDeclarationService(repo.Object);

        var result = await service.GetCompanyDeclarations(TenantId, FY, "APPROVED");

        Assert.True(result.Success);
        Assert.Single(result.MethodResult!);
        Assert.Equal("d1", result.MethodResult![0].Id);
    }

    private static Mock<IMongoDbRepository<TEntity>> Repo<TEntity>(params TEntity[] items)
    {
        var m = new Mock<IMongoDbRepository<TEntity>>();
        m.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool __) => items.FirstOrDefault(p.Compile()));
        m.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool __) => items.Where(p.Compile()).ToArray());
        m.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _) => items.Any(p.Compile()));
        m.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Result { Success = true });
        m.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Result { Success = true });
        return m;
    }

    private static BsonDocument Render(FilterDefinition<EmployeeTaxDeclaration> filter) =>
        filter.Render(new RenderArgs<EmployeeTaxDeclaration>(
            BsonSerializer.SerializerRegistry.GetSerializer<EmployeeTaxDeclaration>(),
            BsonSerializer.SerializerRegistry));
}
