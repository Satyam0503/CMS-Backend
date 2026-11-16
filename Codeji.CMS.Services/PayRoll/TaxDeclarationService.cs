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

        if (!IsValidFinancialYear(request.FinancialYear.Trim()))
            return Fail<EmployeeTaxDeclarationDto>(400, "FinancialYear must use the YYYY-YY format.");

        var numericValues = new[]
        {
            request.PreviousEmployerGrossSalary, request.PreviousEmployerTds, request.PreviousEmployerPf,
            request.PreviousEmployerPt, request.OtherIncomeSavingsInterest, request.OtherIncomeOther,
            request.DeclaredSection80C, request.DeclaredSection80Ccd1B, request.DeclaredSection80D,
            request.DeclaredSection24HomeLoanInterest, request.DeclaredHraRentPaid,
            request.DeclaredSection80E, request.DeclaredOtherDeductions
        };
        if (numericValues.Any(value => value < 0m))
            return Fail<EmployeeTaxDeclarationDto>(400, "Tax declaration amounts cannot be negative.");

        if (request.ProofAttachments is { Count: > 50 })
            return Fail<EmployeeTaxDeclarationDto>(400, "A declaration cannot contain more than 50 proof attachments.");
        if (request.ProofAttachments?.Any(p => p is null || string.IsNullOrWhiteSpace(p.Section) ||
                string.IsNullOrWhiteSpace(p.DocumentName) || p.DocumentName.Trim().Length > 200 ||
                p.Amount < 0m || !IsSafeRelativeDocumentPath(p.DocumentUrl)) == true)
            return Fail<EmployeeTaxDeclarationDto>(400, "Proofs need a section, a document name, a non-negative amount, and a safe application-relative document path.");

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

        if (existing is not null && existing.Status is not ("REJECTED" or "DRAFT" or "APPROVED"))
            return Fail<EmployeeTaxDeclarationDto>(409, "A submitted declaration is awaiting review. Wait for a decision before resubmitting.");

        EmployeeTaxDeclaration entity;
        var expectedVersion = existing?.Version;
        var expectedStatus = existing?.Status;
        if (existing is null || existing.Status == "APPROVED")
        {
            entity = new EmployeeTaxDeclaration
            {
                CompanyId = companyId,
                EmployeeId = employeeId,
                FinancialYear = fy,
                Regime = regime,
                Status = "SUBMITTED",
                Version = existing is null ? 1 : existing.Version + 1,
                PreviousVersionId = existing?.Id,
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

        entity.PreviousEmployerGrossSalary = request.PreviousEmployerGrossSalary;
        entity.PreviousEmployerTds = request.PreviousEmployerTds;
        entity.PreviousEmployerPf = request.PreviousEmployerPf;
        entity.PreviousEmployerPt = request.PreviousEmployerPt;

        entity.OtherIncomeSavingsInterest = request.OtherIncomeSavingsInterest;
        entity.OtherIncomeOther = request.OtherIncomeOther;

        entity.DeclaredSection80C = request.DeclaredSection80C;
        entity.DeclaredSection80Ccd1B = request.DeclaredSection80Ccd1B;
        entity.DeclaredSection80D = request.DeclaredSection80D;
        entity.DeclaredSection24HomeLoanInterest = request.DeclaredSection24HomeLoanInterest;
        entity.DeclaredHraRentPaid = request.DeclaredHraRentPaid;
        entity.DeclaredSection80E = request.DeclaredSection80E;
        entity.DeclaredOtherDeductions = request.DeclaredOtherDeductions;

        entity.LandlordPan = request.LandlordPan?.Trim();
        entity.RentalAddress = request.RentalAddress?.Trim();
        entity.IsMetroCity = request.IsMetroCity;
        entity.ApprovedSection80C = 0m;
        entity.ApprovedSection80Ccd1B = 0m;
        entity.ApprovedSection80D = 0m;
        entity.ApprovedSection24HomeLoanInterest = 0m;
        entity.ApprovedHraExemption = 0m;
        entity.ApprovedSection80E = 0m;
        entity.ApprovedOtherDeductions = 0m;
        entity.ReviewedBy = null;
        entity.ReviewedAtUtc = null;
        entity.ReviewRemarks = null;
        entity.RejectionReason = null;

        entity.SubmittedBy = actorId;
        entity.SubmittedAtUtc = DateTime.UtcNow;

        entity.ProofAttachments = request.ProofAttachments?.Select(p => new TaxProofAttachment
            {
                Id = Guid.NewGuid().ToString(),
                Section = p.Section?.Trim() ?? string.Empty,
                DocumentName = p.DocumentName?.Trim() ?? string.Empty,
                DocumentUrl = p.DocumentUrl?.Trim() ?? string.Empty,
                Amount = p.Amount,
                IsVerified = false
            }).ToList() ?? [];

        entity.AuditHistory.Add(new TaxDeclarationAuditEvent
        {
            Action = "DeclarationSubmitted",
            ActorId = actorId,
            OccurredAtUtc = DateTime.UtcNow,
            Notes = $"Submitted tax declaration for FY {fy} under {regime}."
        });

        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            var insertResult = await declarations.AddOne(entity);
            if (!insertResult.Success)
                return FailWrite<EmployeeTaxDeclarationDto>(insertResult, "A declaration version already exists or could not be created. Reload the declaration and try again.");
        }
        else
        {
            var updateResult = await declarations.Update(
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Id, entity.Id) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Version, expectedVersion!.Value) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Status, expectedStatus!),
                entity);
            if (!updateResult.Success)
                return FailWrite<EmployeeTaxDeclarationDto>(updateResult, "The declaration changed while you were submitting it. Reload the latest version before trying again.");
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

        if (declaration.Status != "SUBMITTED")
            return Fail<EmployeeTaxDeclarationDto>(409, "Only submitted declarations can be reviewed.");
        if (string.Equals(declaration.SubmittedBy, reviewerId, StringComparison.OrdinalIgnoreCase))
            return Fail<EmployeeTaxDeclarationDto>(403, "The submitter cannot review their own tax declaration.");
        var reviewAmounts = new[]
        {
            request.ApprovedSection80C, request.ApprovedSection80Ccd1B, request.ApprovedSection80D,
            request.ApprovedSection24HomeLoanInterest, request.ApprovedHraExemption,
            request.ApprovedSection80E, request.ApprovedOtherDeductions
        };
        if (reviewAmounts.Any(value => value < 0m))
            return Fail<EmployeeTaxDeclarationDto>(400, "Approved tax amounts cannot be negative.");

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

            var updateResult = await declarations.Update(
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Id, declaration.Id) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Version, declaration.Version) &
                Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Status, "SUBMITTED"),
                declaration);
            if (!updateResult.Success)
                return FailWrite<EmployeeTaxDeclarationDto>(updateResult, "This declaration was already updated by another reviewer. Reload its current status.");
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
            declaration.ApprovedSection80C = Math.Min(Math.Min(150000m, declaration.DeclaredSection80C), request.ApprovedSection80C ?? declaration.DeclaredSection80C);
            declaration.ApprovedSection80Ccd1B = Math.Min(Math.Min(50000m, declaration.DeclaredSection80Ccd1B), request.ApprovedSection80Ccd1B ?? declaration.DeclaredSection80Ccd1B);
            declaration.ApprovedSection80D = Math.Min(Math.Min(100000m, declaration.DeclaredSection80D), request.ApprovedSection80D ?? declaration.DeclaredSection80D);
            declaration.ApprovedSection24HomeLoanInterest = Math.Min(Math.Min(200000m, declaration.DeclaredSection24HomeLoanInterest), request.ApprovedSection24HomeLoanInterest ?? declaration.DeclaredSection24HomeLoanInterest);
            declaration.ApprovedHraExemption = Math.Min(declaration.DeclaredHraRentPaid, request.ApprovedHraExemption ?? 0m);
            declaration.ApprovedSection80E = Math.Min(declaration.DeclaredSection80E, request.ApprovedSection80E ?? declaration.DeclaredSection80E);
            declaration.ApprovedOtherDeductions = Math.Min(declaration.DeclaredOtherDeductions, request.ApprovedOtherDeductions ?? declaration.DeclaredOtherDeductions);
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

        var approvalUpdateResult = await declarations.Update(
            Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Id, declaration.Id) &
            Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Version, declaration.Version) &
            Builders<EmployeeTaxDeclaration>.Filter.Eq(x => x.Status, "SUBMITTED"),
            declaration);
        if (!approvalUpdateResult.Success)
            return FailWrite<EmployeeTaxDeclarationDto>(approvalUpdateResult, "This declaration was already updated by another reviewer. Reload its current status.");
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

        var taxPeriod = PayrollTaxPeriod.TryResolveFinancialYear(financialYear);
        if (taxPeriod is null)
            return Fail<TaxProjectionResultDto>(400, "FinancialYear must use the YYYY-YY format.");
        if (!taxPeriod.HasSupportedTaxRules)
            return Fail<TaxProjectionResultDto>(422, $"Automatic tax projection rules for {taxPeriod.FinancialYear} are not available. Use an approved manual calculation or update the statutory rule catalog.");

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

        var taxRules = taxPeriod.TaxRuleSet!;
        var stdDeduction = isNewRegime ? taxRules.NewRegimeStandardDeduction : taxRules.OldRegimeStandardDeduction;
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
            otherDeductions,
            taxRules);

        var cess = PayrollCalculationRules.RoundMoney(totalAnnualTax * 4m / 104m);
        var taxBeforeCess = totalAnnualTax - cess;
        var tdsPeriod = PayrollTaxPeriod.ResolveIndiaSalaryPayment(DateTime.UtcNow);
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        int remainingMonths = ((tdsPeriod.EndDateExclusive.Year - currentMonth.Year) * 12) +
                              (tdsPeriod.EndDateExclusive.Month - currentMonth.Month);
        remainingMonths = Math.Clamp(remainingMonths, 1, 12);

        var remainingAnnualTax = Math.Max(0m, totalAnnualTax - previousEmployerTds);
        var monthlyTds = PayrollCalculationRules.RoundMoney(remainingAnnualTax / remainingMonths);

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
            RemainingMonths = remainingMonths,
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
        PreviousVersionId = entity.PreviousVersionId,
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
        ProofAttachments = (entity.ProofAttachments ?? []).Select(p => new TaxProofAttachmentDto
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
        AuditHistory = (entity.AuditHistory ?? []).Select(a => new TaxDeclarationAuditEventDto
        {
            Action = a.Action,
            ActorId = a.ActorId,
            OccurredAtUtc = a.OccurredAtUtc,
            Notes = a.Notes
        }).ToList()
    };

    private static bool IsSafeRelativeDocumentPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var value = path.Trim();
        return value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) &&
               !value.Contains('\\') && !value.Contains(':') && !value.Contains("..", StringComparison.Ordinal) &&
               !value.Any(char.IsControl);
    }

    private static bool IsValidFinancialYear(string value)
    {
        var parts = value.Split('-', StringSplitOptions.TrimEntries);
        return parts.Length == 2 && parts[0].Length == 4 && parts[1].Length == 2 &&
               int.TryParse(parts[0], out var startYear) && int.TryParse(parts[1], out var endYear) &&
               startYear is >= 1900 and <= 9998 && endYear == (startYear + 1) % 100;
    }

    private static Result<T> Ok<T>(T data) => new() { Success = true, MethodResult = data };
    private static Result<T> Fail<T>(int statusCode, string message) => new() { Success = false, StatusCode = statusCode, Message = message };
    private static Result<T> FailWrite<T>(Result writeResult, string conflictMessage)
    {
        if (writeResult.StatusCode is 400 or 409)
            return Fail<T>(409, conflictMessage);
        return Fail<T>(writeResult.StatusCode is > 0 ? writeResult.StatusCode : 500,
            "The declaration could not be saved. Please retry or contact payroll support.");
    }
}
