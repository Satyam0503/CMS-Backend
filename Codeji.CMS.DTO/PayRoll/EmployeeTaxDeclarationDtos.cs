using System;
using System.Collections.Generic;

namespace Codeji.CMS.DTO.PayRoll;

public sealed class EmployeeTaxDeclarationDto
{
    public string Id { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public string Regime { get; set; } = "NEW_115BAC";
    public string Status { get; set; } = "DRAFT";
    public int Version { get; set; } = 1;

    // Previous Employer Income
    public decimal PreviousEmployerGrossSalary { get; set; }
    public decimal PreviousEmployerTds { get; set; }
    public decimal PreviousEmployerPf { get; set; }
    public decimal PreviousEmployerPt { get; set; }

    // Other Income
    public decimal OtherIncomeSavingsInterest { get; set; }
    public decimal OtherIncomeOther { get; set; }

    // Deductions Declared by Employee
    public decimal DeclaredSection80C { get; set; }
    public decimal DeclaredSection80Ccd1B { get; set; }
    public decimal DeclaredSection80D { get; set; }
    public decimal DeclaredSection24HomeLoanInterest { get; set; }
    public decimal DeclaredHraRentPaid { get; set; }
    public decimal DeclaredSection80E { get; set; }
    public decimal DeclaredOtherDeductions { get; set; }

    // Deductions Approved by Reviewer
    public decimal ApprovedSection80C { get; set; }
    public decimal ApprovedSection80Ccd1B { get; set; }
    public decimal ApprovedSection80D { get; set; }
    public decimal ApprovedSection24HomeLoanInterest { get; set; }
    public decimal ApprovedHraExemption { get; set; }
    public decimal ApprovedSection80E { get; set; }
    public decimal ApprovedOtherDeductions { get; set; }

    // House details for HRA
    public string? LandlordPan { get; set; }
    public string? RentalAddress { get; set; }
    public bool IsMetroCity { get; set; }

    // Proof Attachments
    public List<TaxProofAttachmentDto> ProofAttachments { get; set; } = [];

    // Audit and Review
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewRemarks { get; set; }
    public string? RejectionReason { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public List<TaxDeclarationAuditEventDto> AuditHistory { get; set; } = [];
}

public sealed class TaxProofAttachmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsVerified { get; set; }
}

public sealed class TaxDeclarationAuditEventDto
{
    public string Action { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class SubmitTaxDeclarationRequestDto
{
    public required string FinancialYear { get; set; }
    public string Regime { get; set; } = "NEW_115BAC";

    public decimal PreviousEmployerGrossSalary { get; set; }
    public decimal PreviousEmployerTds { get; set; }
    public decimal PreviousEmployerPf { get; set; }
    public decimal PreviousEmployerPt { get; set; }

    public decimal OtherIncomeSavingsInterest { get; set; }
    public decimal OtherIncomeOther { get; set; }

    public decimal DeclaredSection80C { get; set; }
    public decimal DeclaredSection80Ccd1B { get; set; }
    public decimal DeclaredSection80D { get; set; }
    public decimal DeclaredSection24HomeLoanInterest { get; set; }
    public decimal DeclaredHraRentPaid { get; set; }
    public decimal DeclaredSection80E { get; set; }
    public decimal DeclaredOtherDeductions { get; set; }

    public string? LandlordPan { get; set; }
    public string? RentalAddress { get; set; }
    public bool IsMetroCity { get; set; }

    public List<TaxProofAttachmentDto>? ProofAttachments { get; set; }
}

public sealed class ReviewTaxDeclarationRequestDto
{
    public required string Decision { get; set; } // "APPROVE" or "REJECT"
    public string? ReviewRemarks { get; set; }
    public string? RejectionReason { get; set; }

    public decimal? ApprovedSection80C { get; set; }
    public decimal? ApprovedSection80Ccd1B { get; set; }
    public decimal? ApprovedSection80D { get; set; }
    public decimal? ApprovedSection24HomeLoanInterest { get; set; }
    public decimal? ApprovedHraExemption { get; set; }
    public decimal? ApprovedSection80E { get; set; }
    public decimal? ApprovedOtherDeductions { get; set; }

    public List<string>? VerifiedAttachmentIds { get; set; }
}

public sealed class TaxProjectionResultDto
{
    public string FinancialYear { get; set; } = string.Empty;
    public string Regime { get; set; } = "NEW_115BAC";
    public decimal GrossSalaryFromThisEmployer { get; set; }
    public decimal PreviousEmployerGrossSalary { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal TotalGrossIncome { get; set; }

    public decimal StandardDeduction { get; set; }
    public decimal Section80C { get; set; }
    public decimal Section80Ccd1B { get; set; }
    public decimal Section80D { get; set; }
    public decimal Section24HomeLoanLoss { get; set; }
    public decimal HraExemption { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    public decimal NetTaxableIncome { get; set; }
    public decimal AnnualTaxBeforeCess { get; set; }
    public decimal Section87ARebate { get; set; }
    public decimal HealthAndEducationCess { get; set; }
    public decimal TotalAnnualTax { get; set; }
    public decimal PreviousEmployerTds { get; set; }
    public decimal YtdTaxDeductedByThisEmployer { get; set; }
    public decimal RemainingAnnualTax { get; set; }
    public int RemainingMonths { get; set; }
    public decimal MonthlyTds { get; set; }
}
