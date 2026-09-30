using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public sealed class CreatePayrollAdjustmentRequestDto
{
    public required string OriginalPayrollRunId { get; set; }
    public required string EmployeeId { get; set; }
    public string Type { get; set; } = "CORRECTION";
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
}

public sealed class PayrollAdjustmentActionDto
{
    public string? CorrectionPayrollRunId { get; set; }
}

public interface IPayrollAdjustmentService
{
    Task<Result<PayrollAdjustment>> Create(string companyId, string actor, CreatePayrollAdjustmentRequestDto request);
    Task<Result> Approve(string companyId, string actor, string id);
    Task<Result> Process(string companyId, string actor, string id, PayrollAdjustmentActionDto request);
}

public sealed class PayrollAdjustmentService(
    IMongoDbRepository<PayrollAdjustment> adjustments,
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> employees) : IPayrollAdjustmentService
{
    public async Task<Result<PayrollAdjustment>> Create(string companyId, string actor, CreatePayrollAdjustmentRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.EmployeeId) ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Amount == 0 ||
            request.Type is not ("ARREARS" or "CORRECTION" or "REVERSAL"))
            return Fail<PayrollAdjustment>(400, "Adjustment type, reason, employee and non-zero amount are required.");

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

        adjustment.Status = "APPROVED";
        adjustment.ApprovedBy = actor;
        adjustment.ApprovedAtUtc = DateTime.UtcNow;
        adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent
        {
            Action = "Approved", ActorId = actor, OccurredAtUtc = DateTime.UtcNow
        });
        return await adjustments.Update(
            Builders<PayrollAdjustment>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Id, id) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Status, "DRAFT"),
            adjustment);
    }

    public async Task<Result> Process(string companyId, string actor, string id, PayrollAdjustmentActionDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.CorrectionPayrollRunId))
            return Fail(400, "A correction payroll run is required.");

        var adjustment = await adjustments.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (adjustment is null) return Fail(404, "Adjustment was not found.");
        if (adjustment.Status != "APPROVED") return Fail(409, "Adjustment must be approved before processing.");

        var correctionRun = await runs.FirstOrDefault(x =>
            x.CompanyId == companyId && x.Id == request.CorrectionPayrollRunId && x.RunType == "CORRECTION");
        if (correctionRun is null) return Fail(404, "Correction payroll run was not found.");
        if (correctionRun.Status is not ("CALCULATED" or "PROCESSED"))
            return Fail(409, "The correction payroll run must be calculated before the adjustment can be processed.");
        if (!await employees.Exist(x =>
            x.CompanyId == companyId && x.PayrollRunId == correctionRun.Id &&
            x.EmployeeId == adjustment.EmployeeId && x.Status == "CALCULATED"))
            return Fail(409, "The correction payroll run must contain a calculated row for the adjusted employee.");

        adjustment.Status = "PROCESSED";
        adjustment.CorrectionPayrollRunId = correctionRun.Id;
        adjustment.ProcessedBy = actor;
        adjustment.ProcessedAtUtc = DateTime.UtcNow;
        adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent
        {
            Action = "Processed", ActorId = actor, OccurredAtUtc = DateTime.UtcNow, Notes = correctionRun.Id
        });
        return await adjustments.Update(
            Builders<PayrollAdjustment>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Id, id) &
            Builders<PayrollAdjustment>.Filter.Eq(x => x.Status, "APPROVED"),
            adjustment);
    }

    private static Result Fail(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };

    private static Result<T> Fail<T>(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };
}
