using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

[BsonIgnoreExtraElements]
public sealed class EmployeeTaxDeclaration
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;

    public required string CompanyId { get; set; }
    public required string EmployeeId { get; set; }
    public required string FinancialYear { get; set; } // e.g. "2025-26", "2026-27"
    public string Regime { get; set; } = "NEW_115BAC"; // "NEW_115BAC" or "OLD_REGIME"
    public string Status { get; set; } = "DRAFT"; // "DRAFT", "SUBMITTED", "APPROVED", "REJECTED"
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
    public decimal DeclaredSection80Ccd1B { get; set; } // NPS Employee
    public decimal DeclaredSection80D { get; set; } // Medical Insurance
    public decimal DeclaredSection24HomeLoanInterest { get; set; } // Loss from House Property
    public decimal DeclaredHraRentPaid { get; set; }
    public decimal DeclaredSection80E { get; set; } // Education Loan Interest
    public decimal DeclaredOtherDeductions { get; set; }

    // Deductions Approved by Reviewer (capped at statutory limits)
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
    public List<TaxProofAttachment> ProofAttachments { get; set; } = [];

    // Audit and Review
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewRemarks { get; set; }
    public string? RejectionReason { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public List<TaxDeclarationAuditEvent> AuditHistory { get; set; } = [];
}

public sealed class TaxProofAttachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Section { get; set; } = string.Empty; // e.g. "80C", "80D", "HRA", "PREV_EMPLOYER"
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsVerified { get; set; }
}

public sealed class TaxDeclarationAuditEvent
{
    public required string Action { get; set; }
    public required string ActorId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
