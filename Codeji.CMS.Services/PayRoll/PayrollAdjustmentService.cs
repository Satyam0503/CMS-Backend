using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public sealed class CreatePayrollAdjustmentRequestDto
{
    public required string OriginalPayrollRunId { get; set; }
    public required string EmployeeId { get; set; }
    public required string ComponentCode { get; set; }
    public DateTime TargetPeriod { get; set; }
    public string Type { get; set; } = "CORRECTION";
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
}

public sealed class PayrollAdjustmentActionDto
{
    public string? CorrectionPayrollRunId { get; set; }
}

public sealed class RejectPayrollAdjustmentRequestDto
{
    public required string Reason { get; set; }
}

public sealed class PayrollAdjustmentListItemDto
{
    public required string Id { get; set; }
    public required string OriginalPayrollRunId { get; set; }
    public string? CorrectionPayrollRunId { get; set; }
    public required string EmployeeId { get; set; }
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public string? ComponentKind { get; set; }
    public bool? IsTaxable { get; set; }
    public int? ComponentVersion { get; set; }
    public DateTime? TargetPeriod { get; set; }
    public required string Type { get; set; }
    public required string Status { get; set; }
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}

public interface IPayrollAdjustmentService
{
    Task<Result<List<PayrollAdjustmentListItemDto>>> GetAll(string companyId);
    Task<Result<List<PayrollSalaryComponentDto>>> GetComponents(string companyId, DateTime effectiveDate);
    Task<Result<PayrollAdjustment>> Create(string companyId, string actor, CreatePayrollAdjustmentRequestDto request);
    Task<Result> Approve(string companyId, string actor, string id);
    Task<Result> Reject(string companyId, string actor, string id, RejectPayrollAdjustmentRequestDto request);
    Task<Result> Process(string companyId, string actor, string id, PayrollAdjustmentActionDto request);
}

public sealed class PayrollAdjustmentService(
    IMongoDbRepository<PayrollAdjustment> adjustments,
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> employees,
    IMongoDbRepository<PayrollSalaryComponentDefinition>? componentDefinitions = null) : IPayrollAdjustmentService
{
    public async Task<Result<List<PayrollAdjustmentListItemDto>>> GetAll(string companyId)
    {
        var records = await adjustments.GetAll(x => x.CompanyId == companyId);
        var result = records.OrderByDescending(x => x.CreatedAtUtc).Select(x => new PayrollAdjustmentListItemDto
        {
            Id = x.Id, OriginalPayrollRunId = x.OriginalPayrollRunId, CorrectionPayrollRunId = x.CorrectionPayrollRunId,
            EmployeeId = x.EmployeeId, Type = x.Type, Status = x.Status, Amount = x.Amount, Reason = x.Reason,
            ComponentCode = x.ComponentCode, ComponentName = x.ComponentName, ComponentKind = x.ComponentKind,
            IsTaxable = x.IsTaxable, ComponentVersion = x.ComponentVersion, TargetPeriod = x.TargetPeriod,
            CreatedBy = x.CreatedBy, CreatedAtUtc = x.CreatedAtUtc, ApprovedBy = x.ApprovedBy,
            ApprovedAtUtc = x.ApprovedAtUtc, RejectedBy = x.RejectedBy, RejectedAtUtc = x.RejectedAtUtc,
            RejectionReason = x.RejectionReason, ProcessedBy = x.ProcessedBy, ProcessedAtUtc = x.ProcessedAtUtc
        }).ToList();
        return new Result<List<PayrollAdjustmentListItemDto>> { Success = true, MethodResult = result };
    }

    public async Task<Result<List<PayrollSalaryComponentDto>>> GetComponents(string companyId, DateTime effectiveDate)
    {
        if (componentDefinitions is null)
            return Fail<List<PayrollSalaryComponentDto>>(503, "Payroll component storage is unavailable.");
        if (effectiveDate == default)
            return Fail<List<PayrollSalaryComponentDto>>(400, "A target payroll month is required.");

        var definitions = (await componentDefinitions.GetAll(x => x.CompanyId == companyId)).ToList();
        var options = definitions.Select(x => PayrollComponentDefinitionResolver.Resolve(definitions, companyId, x.Code, effectiveDate))
            .Where(x => x is not null).DistinctBy(x => x!.Code, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PayrollSalaryComponentDto
            {
                Id = x!.Id, Code = x.Code, Name = x.Name, Kind = x.Kind, CalculationType = x.CalculationType,
                BaseComponentCode = x.BaseComponentCode, IsTaxable = x.IsTaxable, EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo, IsActive = x.IsActive, Version = x.Version
            }).OrderBy(x => x.Name).ToList();
        return new Result<List<PayrollSalaryComponentDto>> { Success = true, MethodResult = options };
    }

    public async Task<Result<PayrollAdjustment>> Create(string companyId, string actor, CreatePayrollAdjustmentRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.EmployeeId) ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Amount == 0 ||
            string.IsNullOrWhiteSpace(request.ComponentCode) || request.TargetPeriod == default || request.TargetPeriod.Day != 1 ||
            request.Type is not ("ARREARS" or "CORRECTION" or "REVERSAL"))
            return Fail<PayrollAdjustment>(400, "Adjustment type, reason, employee, component, target payroll month and non-zero amount are required.");

        if (componentDefinitions is null) return Fail<PayrollAdjustment>(503, "Payroll component storage is unavailable.");
        var componentRows = (await componentDefinitions.GetAll(x => x.CompanyId == companyId)).ToList();
        var component = PayrollComponentDefinitionResolver.Resolve(componentRows, companyId, request.ComponentCode, request.TargetPeriod);
        if (component is null)
            return Fail<PayrollAdjustment>(400, "The selected salary component is not active for the target payroll month.");

        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == request.OriginalPayrollRunId);
        if (run is null) return Fail<PayrollAdjustment>(404, "Original payroll run was not found.");
        if (run.Status is not ("PROCESSED" or "PAID" or "CLOSED"))
            return Fail<PayrollAdjustment>(409, "Adjustments require a processed, paid or closed payroll run.");
        if (!await employees.Exist(x => x.CompanyId == companyId && x.PayrollRunId == request.OriginalPayrollRunId && x.EmployeeId == request.EmployeeId))
            return Fail<PayrollAdjustment>(404, "Employee is not part of the original payroll run.");

        var adjustment = new PayrollAdjustment
        {
            CompanyId = companyId, OriginalPayrollRunId = request.OriginalPayrollRunId,
            EmployeeId = request.EmployeeId, Type = request.Type, Amount = request.Amount,
            ComponentCode = component.Code, ComponentName = component.Name, ComponentKind = component.Kind,
            IsTaxable = component.IsTaxable, ComponentVersion = component.Version, TargetPeriod = request.TargetPeriod.Date,
            Reason = request.Reason.Trim(), CreatedBy = actor, CreatedAtUtc = DateTime.UtcNow
        };
        adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent
        {
            Action = "Created", ActorId = actor, OccurredAtUtc = DateTime.UtcNow, Notes = adjustment.Reason
        });
        var saved = await adjustments.AddOne(adjustment);
        return saved.Success
            ? new Result<PayrollAdjustment> { Success = true, MethodResult = adjustment, ResourceId = adjustment.Id }
            : Fail<PayrollAdjustment>(500, "Adjustment could not be created.");
    }

    public async Task<Result> Approve(string companyId, string actor, string id)
    {
        var adjustment = await adjustments.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (adjustment is null) return Fail(404, "Adjustment was not found.");
        if (adjustment.Status != "DRAFT") return Fail(409, "Only a draft adjustment can be approved.");
        if (!string.IsNullOrWhiteSpace(adjustment.CreatedBy) && string.Equals(adjustment.CreatedBy, actor, StringComparison.OrdinalIgnoreCase))
            return Fail(403, "You cannot approve your own payroll adjustment.");

        adjustment.Status = "APPROVED";
        adjustment.ApprovedBy = actor;
        adjustment.ApprovedAtUtc = DateTime.UtcNow;
        adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent
        {
            Action = "Approved", ActorId = actor, OccurredAtUtc = DateTime.UtcNow
        });
        var updated = await adjustments.Update(
            Builders<PayrollAdjustment>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Id, id) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Status, "DRAFT"),
            adjustment);
        return !updated.Success && updated.StatusCode == 400
            ? Fail(409, "The adjustment changed before approval. Refresh the queue and try again.")
            : updated;
    }

    public async Task<Result> Reject(string companyId, string actor, string id, RejectPayrollAdjustmentRequestDto request)
    {
        var reason = request?.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5 || reason.Length > 500)
            return Fail(400, "A rejection reason between 5 and 500 characters is required.");

        var adjustment = await adjustments.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (adjustment is null) return Fail(404, "Adjustment was not found.");
        if (adjustment.Status != "DRAFT") return Fail(409, "Only a draft adjustment can be rejected.");

        adjustment.Status = "REJECTED";
        adjustment.RejectedBy = actor;
        adjustment.RejectedAtUtc = DateTime.UtcNow;
        adjustment.RejectionReason = reason;
        adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent
        {
            Action = "Rejected", ActorId = actor, OccurredAtUtc = DateTime.UtcNow, Notes = reason
        });
        var updated = await adjustments.Update(
            Builders<PayrollAdjustment>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Id, id) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Status, "DRAFT"),
            adjustment);
        return !updated.Success && updated.StatusCode == 400
            ? Fail(409, "The adjustment changed before rejection. Refresh the queue and try again.")
            : updated;
    }

    public async Task<Result> Process(string companyId, string actor, string id, PayrollAdjustmentActionDto request)
    {
        var adjustment = await adjustments.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (adjustment is null) return Fail(404, "Adjustment was not found.");
        if (adjustment.Status != "APPROVED")
            return Fail(409, "Only an approved adjustment can be processed.");

        if (string.IsNullOrWhiteSpace(request?.CorrectionPayrollRunId))
            return Fail(400, "A correction payroll run ID is required to process the adjustment.");

        var runId = request.CorrectionPayrollRunId.Trim();
        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == runId);
        if (run is null)
            return Fail(404, "Correction payroll run was not found.");

        var employeeRunRow = await employees.FirstOrDefault(x =>
            x.CompanyId == companyId && x.PayrollRunId == runId && x.EmployeeId == adjustment.EmployeeId);
        if (employeeRunRow is null || employeeRunRow.Status != "CALCULATED")
            return Fail(409, "The employee was not calculated in the specified correction payroll run.");

        if (run.Status is not ("PROCESSED" or "PAID" or "CLOSED"))
            return Fail(409,
                "Adjustment processing is unavailable until the approved amount is included in a processed correction-run gross-to-net and statutory calculations. The adjustment remains approved.");

        adjustment.Status = "PROCESSED";
        adjustment.CorrectionPayrollRunId = runId;
        adjustment.ProcessedBy = actor;
        adjustment.ProcessedAtUtc = DateTime.UtcNow;
        adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent
        {
            Action = "Processed",
            ActorId = actor,
            OccurredAtUtc = DateTime.UtcNow,
            Notes = $"Processed in payroll run {runId}"
        });

        var updated = await adjustments.Update(
            Builders<PayrollAdjustment>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Id, id) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Status, "APPROVED"),
            adjustment);

        return !updated.Success && updated.StatusCode == 400
            ? Fail(409, "The adjustment changed before processing. Refresh the queue and try again.")
            : updated;
    }


    private static Result Fail(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };

    private static Result<T> Fail<T>(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };
}
