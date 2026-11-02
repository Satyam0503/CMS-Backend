using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.GenericRepository.Interfaces;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public sealed class TaxDeclarationService(
    IMongoDbRepository<EmployeeTaxDeclaration> declarations,
    IMongoDbRepository<EmpUser>? employees = null) : ITaxDeclarationService
{
    public async Task<Result<EmployeeTaxDeclarationDto>> GetDeclaration(string companyId, string employeeId, string financialYear)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(financialYear))
            return Fail<EmployeeTaxDeclarationDto>(400, "CompanyId, EmployeeId, and FinancialYear are required.");

        var record = (await declarations.GetAll(x =>
            x.CompanyId == companyId &&
            x.EmployeeId == employeeId &&
            x.FinancialYear == financialYear.Trim()))
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        if (record is null)
            return Fail<EmployeeTaxDeclarationDto>(404, $"No tax declaration found for employee '{employeeId}' in financial year '{financialYear}'.");

        return Ok(MapToDto(record));
    }

    public async Task<Result<EmployeeTaxDeclarationDto>> SubmitDeclaration(
        string companyId,
        string employeeId,
        SubmitTaxDeclarationRequestDto request,
        string actorId)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(employeeId))
            return Fail<EmployeeTaxDeclarationDto>(400, "CompanyId and EmployeeId are required.");
        if (request is null || string.IsNullOrWhiteSpace(request.FinancialYear))
            return Fail<EmployeeTaxDeclarationDto>(400, "FinancialYear is required.");

        var regime = string.IsNullOrWhiteSpace(request.Regime) ? "NEW_115BAC" : request.Regime.Trim().ToUpperInvariant();
        if (regime != "NEW_115BAC" && regime != "OLD_REGIME")
            return Fail<EmployeeTaxDeclarationDto>(400, "Invalid tax regime. Choose 'NEW_115BAC' or 'OLD_REGIME'.");

        var fy = request.FinancialYear.Trim();
        var existing = (await declarations.GetAll(x =>
            x.CompanyId == companyId &&
            x.EmployeeId == employeeId &&
            x.FinancialYear == fy))
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        EmployeeTaxDeclaration entity;
        if (existing is null)
        {
            entity = new EmployeeTaxDeclaration
            {
                CompanyId = companyId,
                EmployeeId = employeeId,
                FinancialYear = fy,
                Regime = regime,
                Status = "SUBMITTED",
                Version = 1,
                CreatedBy = actorId,
                CreatedAtUtc = DateTime.UtcNow
            };
        }
        else
        {
            entity = existing;
            entity.Version += 1;
            entity.Regime = regime;
            entity.Status = "SUBMITTED";
            entity.UpdatedBy = actorId;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        entity.PreviousEmployerGrossSalary = Math.Max(0m, request.PreviousEmployerGrossSalary);
        entity.PreviousEmployerTds = Math.Max(0m, request.PreviousEmployerTds);
        entity.PreviousEmployerPf = Math.Max(0m, request.PreviousEmployerPf);
        entity.PreviousEmployerPt = Math.Max(0m, request.PreviousEmployerPt);

        entity.OtherIncomeSavingsInterest = Math.Max(0m, request.OtherIncomeSavingsInterest);
        entity.OtherIncomeOther = Math.Max(0m, request.OtherIncomeOther);

        entity.DeclaredSection80C = Math.Max(0m, request.DeclaredSection80C);
        entity.DeclaredSection80Ccd1B = Math.Max(0m, request.DeclaredSection80Ccd1B);
        entity.DeclaredSection80D = Math.Max(0m, request.DeclaredSection80D);
        entity.DeclaredSection24HomeLoanInterest = Math.Max(0m, request.DeclaredSection24HomeLoanInterest);
        entity.DeclaredHraRentPaid = Math.Max(0m, request.DeclaredHraRentPaid);
        entity.DeclaredSection80E = Math.Max(0m, request.DeclaredSection80E);
        entity.DeclaredOtherDeductions = Math.Max(0m, request.DeclaredOtherDeductions);

        entity.LandlordPan = request.LandlordPan?.Trim();
        entity.RentalAddress = request.RentalAddress?.Trim();
        entity.IsMetroCity = request.IsMetroCity;

        entity.SubmittedBy = actorId;
        entity.SubmittedAtUtc = DateTime.UtcNow;

        if (request.ProofAttachments is not null)
        {
            entity.ProofAttachments = request.ProofAttachments.Select(p => new TaxProofAttachment
            {
                Id = string.IsNullOrWhiteSpace(p.Id) ? Guid.NewGuid().ToString() : p.Id,
                Section = p.Section?.Trim() ?? string.Empty,
                DocumentName = p.DocumentName?.Trim() ?? string.Empty,
                DocumentUrl = p.DocumentUrl?.Trim() ?? string.Empty,
                Amount = Math.Max(0m, p.Amount),
                IsVerified = false
            }).ToList();
        }

        entity.AuditHistory.Add(new TaxDeclarationAuditEvent
        {
            Action = "DeclarationSubmitted",
            ActorId = actorId,
            OccurredAtUtc = DateTime.UtcNow,
            Notes = $"Submitted tax declaration for FY {fy} under {regime}."
        });

        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            await declarations.AddOne(entity);
        }
        else
        {
            await declarations.Update(
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Id, entity.Id),
                entity);
        }

        return Ok(MapToDto(entity));
    }

    public async Task<Result<EmployeeTaxDeclarationDto>> ReviewDeclaration(
        string companyId,
        string declarationId,
        ReviewTaxDeclarationRequestDto request,
        string reviewerId)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(declarationId) || request is null)
            return Fail<EmployeeTaxDeclarationDto>(400, "CompanyId, DeclarationId, and request are required.");

        var declaration = await declarations.FirstOrDefault(x => x.CompanyId == companyId && x.Id == declarationId);
        if (declaration is null)
            return Fail<EmployeeTaxDeclarationDto>(404, "Tax declaration not found.");

        var decision = request.Decision?.Trim().ToUpperInvariant();
        if (decision != "APPROVE" && decision != "REJECT")
            return Fail<EmployeeTaxDeclarationDto>(400, "Decision must be either 'APPROVE' or 'REJECT'.");

        if (decision == "REJECT")
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                return Fail<EmployeeTaxDeclarationDto>(400, "RejectionReason is required when rejecting a tax declaration.");

            declaration.Status = "REJECTED";
            declaration.ReviewedBy = reviewerId;
            declaration.ReviewedAtUtc = DateTime.UtcNow;
            declaration.RejectionReason = request.RejectionReason.Trim();
            declaration.ReviewRemarks = request.ReviewRemarks?.Trim();

            declaration.AuditHistory.Add(new TaxDeclarationAuditEvent
            {
                Action = "DeclarationRejected",
                ActorId = reviewerId,
                OccurredAtUtc = DateTime.UtcNow,
                Notes = $"Rejected declaration: {declaration.RejectionReason}"
            });

            await declarations.Update(
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Id, declaration.Id),
                declaration);
            return Ok(MapToDto(declaration));
        }

        // Decision == APPROVE: Enforce statutory caps
        declaration.Status = "APPROVED";
        declaration.ReviewedBy = reviewerId;
        declaration.ReviewedAtUtc = DateTime.UtcNow;
        declaration.ReviewRemarks = request.ReviewRemarks?.Trim();
        declaration.RejectionReason = null;

        if (declaration.Regime == "NEW_115BAC")
        {
            // Under Section 115BAC, Chapter VI-A deductions are not allowable
            declaration.ApprovedSection80C = 0m;
            declaration.ApprovedSection80Ccd1B = 0m;
            declaration.ApprovedSection80D = 0m;
            declaration.ApprovedSection24HomeLoanInterest = 0m;
            declaration.ApprovedHraExemption = 0m;
            declaration.ApprovedSection80E = 0m;
            declaration.ApprovedOtherDeductions = 0m;
        }
        else
        {
            // Old Regime statutory caps
            declaration.ApprovedSection80C = Math.Min(150000m, Math.Max(0m, request.ApprovedSection80C ?? declaration.DeclaredSection80C));
            declaration.ApprovedSection80Ccd1B = Math.Min(50000m, Math.Max(0m, request.ApprovedSection80Ccd1B ?? declaration.DeclaredSection80Ccd1B));
            declaration.ApprovedSection80D = Math.Min(100000m, Math.Max(0m, request.ApprovedSection80D ?? declaration.DeclaredSection80D));
            declaration.ApprovedSection24HomeLoanInterest = Math.Min(200000m, Math.Max(0m, request.ApprovedSection24HomeLoanInterest ?? declaration.DeclaredSection24HomeLoanInterest));
            declaration.ApprovedHraExemption = Math.Max(0m, request.ApprovedHraExemption ?? 0m);
            declaration.ApprovedSection80E = Math.Max(0m, request.ApprovedSection80E ?? declaration.DeclaredSection80E);
            declaration.ApprovedOtherDeductions = Math.Max(0m, request.ApprovedOtherDeductions ?? declaration.DeclaredOtherDeductions);
        }

        if (request.VerifiedAttachmentIds is not null && request.VerifiedAttachmentIds.Count > 0)
        {
            var verifiedSet = new HashSet<string>(request.VerifiedAttachmentIds, StringComparer.Ordinal);
            foreach (var attachment in declaration.ProofAttachments)
            {
                if (verifiedSet.Contains(attachment.Id))
                    attachment.IsVerified = true;
            }
        }

        declaration.AuditHistory.Add(new TaxDeclarationAuditEvent
        {
            Action = "DeclarationApproved",
            ActorId = reviewerId,
            OccurredAtUtc = DateTime.UtcNow,
            Notes = $"Approved declaration under {declaration.Regime}. Total approved deductions: {declaration.ApprovedSection80C + declaration.ApprovedSection80Ccd1B + declaration.ApprovedSection80D + declaration.ApprovedSection24HomeLoanInterest + declaration.ApprovedHraExemption + declaration.ApprovedSection80E + declaration.ApprovedOtherDeductions:C}"
        });

        await declarations.Update(
            Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Id, declaration.Id),
            declaration);
        return Ok(MapToDto(declaration));
    }

    public async Task<Result<List<EmployeeTaxDeclarationDto>>> GetCompanyDeclarations(
        string companyId,
        string financialYear,
        string? status = null)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(financialYear))
            return Fail<List<EmployeeTaxDeclarationDto>>(400, "CompanyId and FinancialYear are required.");

        var records = await declarations.GetAll(x =>
            x.CompanyId == companyId &&
            x.FinancialYear == financialYear.Trim());

        if (!string.IsNullOrWhiteSpace(status))
        {
            records = records.Where(x => string.Equals(x.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var result = records
            .OrderBy(x => x.EmployeeId)
            .ThenByDescending(x => x.Version)
            .Select(MapToDto)
            .ToList();

        return Ok(result);
    }

    public async Task<Result<TaxProjectionResultDto>> CalculateTaxProjection(
        string companyId,
        string employeeId,
        string financialYear,
        decimal annualGrossSalary)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(financialYear))
            return Fail<TaxProjectionResultDto>(400, "CompanyId, EmployeeId, and FinancialYear are required.");

        var declaration = (await declarations.GetAll(x =>
            x.CompanyId == companyId &&
            x.EmployeeId == employeeId &&
            x.FinancialYear == financialYear.Trim() &&
            x.Status == "APPROVED"))
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        var regime = declaration?.Regime ?? "NEW_115BAC";
        var isNewRegime = regime == "NEW_115BAC";

        var previousEmployerSalary = declaration?.PreviousEmployerGrossSalary ?? 0m;
        var previousEmployerTds = declaration?.PreviousEmployerTds ?? 0m;
        var otherIncome = (declaration?.OtherIncomeSavingsInterest ?? 0m) + (declaration?.OtherIncomeOther ?? 0m);

        var stdDeduction = isNewRegime ? 75000m : 50000m;
        var sec80C = isNewRegime ? 0m : (declaration?.ApprovedSection80C ?? 0m);
        var sec80Ccd1B = isNewRegime ? 0m : (declaration?.ApprovedSection80Ccd1B ?? 0m);
        var sec80D = isNewRegime ? 0m : (declaration?.ApprovedSection80D ?? 0m);
        var sec24Loss = isNewRegime ? 0m : (declaration?.ApprovedSection24HomeLoanInterest ?? 0m);
        var hra = isNewRegime ? 0m : (declaration?.ApprovedHraExemption ?? 0m);
        var otherDeductions = isNewRegime ? 0m : ((declaration?.ApprovedSection80E ?? 0m) + (declaration?.ApprovedOtherDeductions ?? 0m));

        var totalDeductions = stdDeduction + sec80C + sec80Ccd1B + sec80D + sec24Loss + hra + otherDeductions;
        var totalGrossIncome = annualGrossSalary + previousEmployerSalary + otherIncome;
        var netTaxable = Math.Max(0m, totalGrossIncome - totalDeductions);

        var totalAnnualTax = PayrollCalculationRules.CalculateAnnualIncomeTaxWithCess(
            annualGrossSalary,
            isNewRegime ? "AUTO_NEW_REGIME" : "AUTO_OLD_REGIME",
            previousEmployerSalary,
            otherIncome,
            sec80C,
            sec80Ccd1B,
            sec80D,
            sec24Loss,
            hra,
            otherDeductions);

        var cess = PayrollCalculationRules.RoundMoney(totalAnnualTax * 4m / 104m);
        var taxBeforeCess = totalAnnualTax - cess;
        var remainingAnnualTax = Math.Max(0m, totalAnnualTax - previousEmployerTds);
        var monthlyTds = PayrollCalculationRules.RoundMoney(remainingAnnualTax / 12m);

        return Ok(new TaxProjectionResultDto
        {
            FinancialYear = financialYear,
            Regime = regime,
            GrossSalaryFromThisEmployer = annualGrossSalary,
            PreviousEmployerGrossSalary = previousEmployerSalary,
            TotalGrossSalary = annualGrossSalary + previousEmployerSalary,
            OtherIncome = otherIncome,
            TotalGrossIncome = totalGrossIncome,
            StandardDeduction = stdDeduction,
            Section80C = sec80C,
            Section80Ccd1B = sec80Ccd1B,
            Section80D = sec80D,
            Section24HomeLoanLoss = sec24Loss,
            HraExemption = hra,
            OtherDeductions = otherDeductions,
            TotalDeductions = totalDeductions,
            NetTaxableIncome = netTaxable,
            AnnualTaxBeforeCess = taxBeforeCess,
            HealthAndEducationCess = cess,
            TotalAnnualTax = totalAnnualTax,
            PreviousEmployerTds = previousEmployerTds,
            RemainingAnnualTax = remainingAnnualTax,
            RemainingMonths = 12,
            MonthlyTds = monthlyTds
        });
    }

    private static EmployeeTaxDeclarationDto MapToDto(EmployeeTaxDeclaration entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        EmployeeId = entity.EmployeeId,
        FinancialYear = entity.FinancialYear,
        Regime = entity.Regime,
        Status = entity.Status,
        Version = entity.Version,
        PreviousEmployerGrossSalary = entity.PreviousEmployerGrossSalary,
        PreviousEmployerTds = entity.PreviousEmployerTds,
        PreviousEmployerPf = entity.PreviousEmployerPf,
        PreviousEmployerPt = entity.PreviousEmployerPt,
        OtherIncomeSavingsInterest = entity.OtherIncomeSavingsInterest,
        OtherIncomeOther = entity.OtherIncomeOther,
        DeclaredSection80C = entity.DeclaredSection80C,
        DeclaredSection80Ccd1B = entity.DeclaredSection80Ccd1B,
        DeclaredSection80D = entity.DeclaredSection80D,
        DeclaredSection24HomeLoanInterest = entity.DeclaredSection24HomeLoanInterest,
        DeclaredHraRentPaid = entity.DeclaredHraRentPaid,
        DeclaredSection80E = entity.DeclaredSection80E,
        DeclaredOtherDeductions = entity.DeclaredOtherDeductions,
        ApprovedSection80C = entity.ApprovedSection80C,
        ApprovedSection80Ccd1B = entity.ApprovedSection80Ccd1B,
        ApprovedSection80D = entity.ApprovedSection80D,
        ApprovedSection24HomeLoanInterest = entity.ApprovedSection24HomeLoanInterest,
        ApprovedHraExemption = entity.ApprovedHraExemption,
        ApprovedSection80E = entity.ApprovedSection80E,
        ApprovedOtherDeductions = entity.ApprovedOtherDeductions,
        LandlordPan = entity.LandlordPan,
        RentalAddress = entity.RentalAddress,
        IsMetroCity = entity.IsMetroCity,
        ProofAttachments = entity.ProofAttachments.Select(p => new TaxProofAttachmentDto
        {
            Id = p.Id,
            Section = p.Section,
            DocumentName = p.DocumentName,
            DocumentUrl = p.DocumentUrl,
            Amount = p.Amount,
            IsVerified = p.IsVerified
        }).ToList(),
        SubmittedBy = entity.SubmittedBy,
        SubmittedAtUtc = entity.SubmittedAtUtc,
        ReviewedBy = entity.ReviewedBy,
        ReviewedAtUtc = entity.ReviewedAtUtc,
        ReviewRemarks = entity.ReviewRemarks,
        RejectionReason = entity.RejectionReason,
        CreatedBy = entity.CreatedBy,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedBy = entity.UpdatedBy,
        UpdatedAtUtc = entity.UpdatedAtUtc,
        AuditHistory = entity.AuditHistory.Select(a => new TaxDeclarationAuditEventDto
        {
            Action = a.Action,
            ActorId = a.ActorId,
            OccurredAtUtc = a.OccurredAtUtc,
            Notes = a.Notes
        }).ToList()
    };

    private static Result<T> Ok<T>(T data) => new() { Success = true, MethodResult = data };
    private static Result<T> Fail<T>(int statusCode, string message) => new() { Success = false, StatusCode = statusCode, Message = message };
}
